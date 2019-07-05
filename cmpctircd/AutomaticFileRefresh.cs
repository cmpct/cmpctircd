using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace cmpctircd {
    public abstract class AutomaticFileRefresh : IDisposable {
        private readonly FileInfo _target; // Target file
        private readonly FileSystemWatcher _watcher; // File watcher
        protected bool Reload { get; private set; } = true; // Whether the file needs to be reloaded or not
        private readonly int _retries; // File lock retries
        private readonly int _delay; // File lock wait delay
        protected readonly SemaphoreSlim semaphore = new SemaphoreSlim(1); // Reading semaphore (to prevent two refresh requests)

        /// <summary>
        /// Constructor for AutomaticFileRefresh
        /// </summary>
        /// <param name="target">Target file.</param>
        /// <param name="retries">Number of retries to read a lockes file.</param>
        /// <param name="delay">Time to wait between attempts.</param>
        public AutomaticFileRefresh(FileInfo target, int retries = -1, int delay = 100) {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _retries = retries;
            _delay = delay;
            // Setup file watch
            _watcher = new FileSystemWatcher(_target.DirectoryName, _target.Name);
            _watcher.Changed += Handler;
            _watcher.Created += Handler;
            _watcher.Deleted += Handler;
            _watcher.Renamed += Handler;
            _watcher.EnableRaisingEvents = true;
        }

        private void Handler(object sender, FileSystemEventArgs e) {
            // Set reload value to true, reading will occur on request to avoid file lock.
            Reload = true;
        }

        /// <summary>
        /// Reloads the file cache.
        /// </summary>
        /// <returns>Task indicating the completion of the operation.</returns>
        protected async Task<byte[]> Read() {
            int attempts = 0;
            byte[] contents;
            while(true) {
                try {
                    using(FileStream stream = _target.OpenRead()) {
                        await stream.ReadAsync(contents = new byte[(int)stream.Length], 0, (int) stream.Length); // int will support up to 3GB
                        Reload = false;
                        return contents;
                    }
                } catch(IOException e) {
                    // IOException.HResult used for cross-platform compatibility. This field is unprotected in .NET 4.5+
                    // <https://github.com/dotnet/corefx/issues/11144>
                    // Error 32 and 33 are file lock errors, see below:
                    // <https://docs.microsoft.com/en-us/windows/desktop/Debug/system-error-codes--0-499->
                    int code = e.HResult & ((1 << 16) - 1);
                    if(code == 32 || code == 33) { // 
                        if(_retries > 0 && attempts++ >= _retries) throw;
                        await Task.Delay(_delay); // So we don't spam the OS with read requests.
                    } else {
                        throw;
                    }
                }
            }
        }

        #region IDisposable Support
        protected bool Disposed { get; private set; } = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!Disposed) {
                if (disposing) {
                    semaphore.Dispose();
                    _watcher.Dispose();
                }
                Disposed = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() {
            Dispose(true);
        }
        #endregion

    }

    /// <summary>
    /// Watches a specified file for changes and provides up-to-date contents with caching.
    /// </summary>
    public sealed class AutomaticFileCacheRefresh : AutomaticFileRefresh {
        private byte[] _cache = new byte[0]; // File contents cache

        /// <summary>
        /// Constructor for AutomaticFileCacheRefresh
        /// </summary>
        /// <param name="target">Target file.</param>
        /// <param name="retries">Number of retries to read a lockes file.</param>
        /// <param name="delay">Time to wait between attempts.</param>
        public AutomaticFileCacheRefresh(FileInfo target, int retries = -1, int delay = 100) : base(target, retries, delay) { }

        /// <summary>
        /// Gets a Stream of the file contents.
        /// </summary>
        /// <returns>Stream of the file contents.</returns>
        public async Task<Stream> GetStreamAsync() {
            await semaphore.WaitAsync();
            if(Reload) _cache = await Read();
            semaphore.Release();
            return new MemoryStream(_cache, 0, _cache.Length, false, false); // No writing or byte array access.
        }
    }

    /// <summary>
    /// Watches a specified certificate file for changes and provides up-to-date contents with caching.
    /// </summary>
    public sealed class AutomaticCertificateCacheRefresh : AutomaticFileRefresh {
        private X509Certificate2 _certificate; // Certificate cache
        private readonly string _password = "";

        /// <summary>
        /// Constructor for AutomaticCertificateCacheRefresh
        /// </summary>
        /// <param name="target">Target file.</param>
        /// <param name="retries">Number of retries to read a lockes file.</param>
        /// <param name="delay">Time to wait between attempts.</param>
        /// <param name="password">Password for the certificate file.</param>
        public AutomaticCertificateCacheRefresh(FileInfo target, int retries = -1, int delay = 100, string password = "") : base(target, retries, delay) {
            _password = password ?? throw new ArgumentNullException(nameof(password));
        }

        /// <summary>
        /// Gets a X509Certificate2 of the current certificate file contents.
        /// </summary>
        /// <returns>The certificate as an X509Certificate2.</returns>
        public async Task<X509Certificate2> GetCertificateAsync() {
            await semaphore.WaitAsync();
            if(Reload) {
                _certificate = new X509Certificate2(await Read(), _password, X509KeyStorageFlags.DefaultKeySet);

                // Won't run on Mono
                //_certificate.Reset();
                //_certificate.Import(await Read(), _password, X509KeyStorageFlags.DefaultKeySet);
            }
            semaphore.Release();
            return _certificate;
        }

        protected override void Dispose(bool disposing) {
            if(!Disposed && disposing)
                _certificate.Dispose();
            base.Dispose(disposing);
        }
    }
}

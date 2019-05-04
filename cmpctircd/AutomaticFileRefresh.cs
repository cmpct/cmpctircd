using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace cmpctircd {
    /// <summary>
    /// Watches a specified file for changes and provides up-to-date contents with caching.
    /// </summary>
    public class AutomaticFileRefresh : IDisposable {
        private readonly FileInfo _target; // Target file
        private readonly FileSystemWatcher _watcher; // File watcher
        private byte[] _cache = new byte[0]; // File contents cache
        private bool _reload = true; // Whether the file needs to be reloaded or not
        private readonly int _retries; // File lock retries
        private readonly int _delay; // File lock wait delay
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1); // Reading semaphore (to prevent two refresh requests)

        /// <summary>
        /// Constructor for AutomaticFileRefresh
        /// </summary>
        /// <param name="target">Target file.</param>
        /// <param name="retries">Number of retries to read a lockes file.</param>
        /// <param name="delay">Time to wait between attempts.</param>
        public AutomaticFileRefresh(FileInfo target, int retries = -1, int delay = 100) {
            _target = target;
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
            _reload = true;
        }

        /// <summary>
        /// Reloads the file cache.
        /// </summary>
        /// <returns>Task indicating the completion of the operation.</returns>
        private async Task Reload() {
            int attempts = 0;
            while(true) {
                try {
                    using(FileStream stream = _target.OpenRead()) {
                        await stream.ReadAsync(_cache = new byte[(int) stream.Length], 0, (int) stream.Length); // int will support up to 3GB
                        _reload = false;
                        break;
                    }
                } catch(IOException e) {
                    // IOException.HResult used for cross-platform compatibility. This field is unprotected in .NET 4.5+
                    // <https://github.com/dotnet/corefx/issues/11144>
                    // Error 32 and 33 are file lock errors, see below:
                    // <https://docs.microsoft.com/en-us/windows/desktop/Debug/system-error-codes--0-499->
                    int code = e.HResult & ((1 << 16) - 1);
                    if(code == 32 || code == 33) { // 
                        if(_retries >= 0 && attempts++ >= _retries) throw;
                        await Task.Delay(_delay); // So we don't spam the OS with read requests.
                    } else {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Gets a Stream of the file contents.
        /// </summary>
        /// <returns>Stream of the file contents.</returns>
        public async Task<Stream> GetStreamAsync() {
            await _semaphore.WaitAsync();
            if(_reload) await Reload();
            _semaphore.Release();
            return new MemoryStream(_cache, 0, _cache.Length, false, false); // No writing or byte array access.
        }

        #region IDisposable Support
        private bool disposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposed) {
                if (disposing) {
                    _semaphore.Dispose();
                    _watcher.Dispose();
                }
                disposed = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() {
            Dispose(true);
        }
        #endregion

    }
}

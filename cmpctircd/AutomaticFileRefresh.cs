using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace cmpctircd {
    /// <summary>
    /// Watches a specified file for changes and provides up-to-date contents with caching.
    /// </summary>
    public class AutomaticFileRefresh {
        private readonly FileInfo _target; // Target file
        private readonly FileSystemWatcher _watcher; // File watcher
        private string[] _cache = new string[0]; // File contents cache
        private bool _reload = true; // Whether the file needs to be reloaded or not
        private int _retries; // File lock retries
        private int _delay; // File lock wait delay
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1); // Reading semaphore (to prevent two refresh requests)

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
                    using(StreamReader reader = _target.OpenText()) {
                        _cache = (await reader.ReadToEndAsync()).Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                        _reload = false;
                        break;
                    }
                } catch(IOException e) {
                    // See <https://docs.microsoft.com/en-us/windows/desktop/Debug/system-error-codes--0-499->
                    // Error 32 and 33 are file lock errors.
                    int code = System.Runtime.InteropServices.Marshal.GetHRForException(e) & ((1 << 16) - 1);
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
        /// Gets all lines of the file, in a string array, asynchronously.
        /// </summary>
        /// <returns>A Task tracking the completion of the operation, resulting in a string array of file lines.</returns>
        public async Task<string[]> GetAllLinesAsync() {
            await _semaphore.WaitAsync();
            if(_reload == false) {
                _semaphore.Release();
                return _cache;
            } else {
                await Reload();
                _semaphore.Release();
                return _cache;
            }
        }

    }
}

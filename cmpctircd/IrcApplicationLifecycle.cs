using System.Collections.Generic;
using cmpctircd.Configuration.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace cmpctircd {
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using cmpctircd.Configuration;
    using cmpctircd.Threading;
    using Microsoft.Extensions.Hosting;

    public class IrcApplicationLifecycle : IHostedService {
        private readonly IRCd ircd;
        private readonly Log log;
        private readonly IConfiguration config;
        private readonly IHostApplicationLifetime appLifetime;
        private QueuedSynchronizationContext synchronizationContext;
        private IOptions<LoggerOptions> _loggerOptions;

        public IrcApplicationLifecycle(IRCd ircd, Log log, IConfiguration config, IHostApplicationLifetime appLifetime, IOptions<LoggerOptions> loggerOptions) {
            _loggerOptions = loggerOptions;
            this.ircd = ircd ?? throw new ArgumentNullException(nameof(ircd));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.appLifetime = appLifetime ?? throw new ArgumentNullException(nameof(appLifetime));
        }

        public Task StartAsync(CancellationToken cancellationToken) {
            appLifetime.ApplicationStarted.Register(OnStarted);
            appLifetime.ApplicationStopping.Register(OnStopping);
            appLifetime.ApplicationStopped.Register(OnStopped);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

        private void OnStarted() {
            synchronizationContext = new QueuedSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);
            log.Initialise(ircd, _loggerOptions.Value.Loggers.ToList());
            ircd.Run();
            synchronizationContext.Run();
        }

        private void OnStopping() {
            ircd.Stop();
            synchronizationContext.Stop();
        }

        private void OnStopped() {

        }
    }
}

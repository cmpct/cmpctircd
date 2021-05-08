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
        private readonly CmpctConfigurationSection config;
        private readonly IHostApplicationLifetime appLifetime;
        private QueuedSynchronizationContext synchronizationContext;

        public IrcApplicationLifecycle(IRCd ircd, Log log, CmpctConfigurationSection config, IHostApplicationLifetime appLifetime) {
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
            log.Initialise(ircd, config.Loggers.OfType<LoggerElement>().ToList());
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

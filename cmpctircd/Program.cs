namespace cmpctircd {
    using cmpctircd.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    class Program {
        static void Main(string[] args) {
            CreateHostBuilder(args)
                .Build().Run();
        }

        static IHostBuilder CreateHostBuilder(string[] args) {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) => {
                    var log = new Log();
                    var config = CmpctConfigurationSection.GetConfiguration();
                    services.AddSingleton(config);
                    services.AddSingleton(log);
                    services.AddSingleton<IRCd>();
                    services.AddHostedService<IrcApplicationLifecycle>();
                });
        }
    }
 }

using System.IO;
using Microsoft.Extensions.Configuration;

namespace cmpctircd {
    using System;
    using System.Linq;
    using cmpctircd.Configuration;
    using cmpctircd.Controllers;
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
                    var configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                        .AddJsonFile("appsettings.json", false)
                        .Build();

                    foreach (var controllerType in AppDomain.CurrentDomain.GetAssemblies().SelectMany(t => t.GetTypes()).Where(t => !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t))) {
                        services.AddTransient(controllerType);
                    }

                    services.AddSingleton(configuration);
                    services.AddSingleton(log);
                    services.AddSingleton<IRCd>();
                    services.AddScoped<IrcContext>();
                    services.AddScoped(sp => sp.GetRequiredService<IrcContext>().Sender as Client);
                    services.AddScoped(sp => sp.GetRequiredService<IrcContext>().Sender as Server);
                    services.AddHostedService<IrcApplicationLifecycle>();
                });
        }
    }
 }

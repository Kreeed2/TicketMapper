using System;
using System.IO;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using SyncApp.Models;
using SyncApp.Services;
using SyncApp.Interfaces;

namespace SyncApp
{
    public class Program
    {
        public class Options
        {
            [Option("dry-run", Required = false, HelpText = "Run without making any changes to target systems.")]
            public bool DryRun { get; set; }
        }

        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o =>
                {
                    CreateHostBuilder(args, o).Build().Run();
                });
        }

        public static IHostBuilder CreateHostBuilder(string[] args, Options options) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Load Config
                    var configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
                    if (!File.Exists(configPath))
                    {
                        // Fallback or error
                        throw new FileNotFoundException("config.json not found", configPath);
                    }
                    var configJson = File.ReadAllText(configPath);
                    var appConfig = JsonConvert.DeserializeObject<AppConfiguration>(configJson);

                    if (appConfig == null) throw new InvalidOperationException("Failed to load configuration.");

                    services.AddSingleton(appConfig);
                    services.AddSingleton<ITransformer, Transformer>();
                    services.AddSingleton<ClientFactory>();
                    services.AddHttpClient();

                    // Register Worker with options
                    services.AddHostedService<Worker>(provider =>
                        new Worker(
                            provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Worker>>(),
                            provider.GetRequiredService<ClientFactory>(),
                            provider.GetRequiredService<ITransformer>(),
                            appConfig,
                            options.DryRun
                        ));
                });
    }
}

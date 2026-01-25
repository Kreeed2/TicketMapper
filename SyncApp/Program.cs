using CommandLine;
using RestSharp;
using SyncApp.Models;
using SyncApp.Services;
using SyncApp.Services.Devops;
using System.Text.Json;

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
                    var appConfig = JsonSerializer.Deserialize<AppConfiguration>(configJson)
                        ?? throw new InvalidOperationException("Failed to load configuration.");
                    services.AddSingleton(appConfig);
                    services.AddSingleton<IRestClient>(provider =>
                    {
                        // Default RestClient without a base URL; ClientFactory can create configured clients per system.
                        return new RestClient(new RestClientOptions
                        {
                            ThrowOnAnyError = false,
                            FollowRedirects = true
                        });
                    });

                    services.AddSingleton<ClientFactory>();
                    services.AddSingleton<TransformerFactory>();
                    services.AddSingleton<UserTransformer>();
                    services.AddSingleton<TextTransformer>();

                    services.AddSingleton(provider =>
                    {
                        var adoSystem = appConfig.Systems
                            .FirstOrDefault(s => s.Value.Type == SystemMappingType.AzureDevOps)
                            .Value;

                        return new AzureDevOpsUserService(
                            adoSystem,
                            provider.GetRequiredService<IRestClient>(),
                            provider.GetRequiredService<ILogger<AzureDevOpsUserService>>()
                        );
                    });

                    // Register Worker with options
                    services.AddHostedService(provider =>
                        new Worker(
                            provider.GetRequiredService<ILogger<Worker>>(),
                            provider.GetRequiredService<ClientFactory>(),
                            provider.GetRequiredService<TransformerFactory>(),
                            appConfig,
                            options.DryRun
                        ));
                });
    }
}

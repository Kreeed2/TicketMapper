using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Graph.Client;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using RestSharp;
using SyncApp.Interfaces;
using SyncApp.Models;
using SyncApp.Services;

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
                    var appConfig = JsonConvert.DeserializeObject<AppConfiguration>(configJson)
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

                    // ADO Graph API für User-Lookup
                    services.AddSingleton(provider =>
                    {
                        var appConfig = provider.GetRequiredService<AppConfiguration>();
                        if (!appConfig.Systems.TryGetValue("ado_dev", out var adoConfig))
                        {
                            throw new InvalidOperationException("Azure DevOps configuration not found.");
                        }

                        var credentials = new VssBasicCredential(string.Empty, adoConfig.Token);
                        var connection = new VssConnection(new Uri(adoConfig.Url), credentials);
                        return connection.GetClient<GraphHttpClient>();
                    });

                    services.AddSingleton<ITransformer, TextTransformer>();
                    services.AddSingleton<ITransformer, UserTransformer>();

                    services.AddSingleton<ClientFactory>();
                    services.AddSingleton<TransformerFactory>();

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

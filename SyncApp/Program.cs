using CommandLine;
using Newtonsoft.Json;
using RestSharp;
using SyncApp.Models;
using SyncApp.Services;
using SyncApp.Interfaces;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;

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

                    //// Optional: register a delegate factory to create RestClient instances with a specific base URL
                    //services.AddSingleton<Func<string, RestClient>>(provider => baseUrl =>
                    //{
                    //    var options = new RestClientOptions
                    //    {
                    //        BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? null : new Uri(baseUrl),
                    //        ThrowOnAnyError = false,
                    //        FollowRedirects = true
                    //    };
                    //    return new RestClient(options);
                    //});

                    services.AddSingleton<ITransformer, Transformer>();
                    services.AddSingleton<ClientFactory>();

                    // Register Worker with options
                    services.AddHostedService(provider =>
                        new Worker(
                            provider.GetRequiredService<ILogger<Worker>>(),
                            provider.GetRequiredService<ClientFactory>(),
                            provider.GetRequiredService<ITransformer>(),
                            appConfig,
                            options.DryRun
                        ));
                });
    }
}

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Azure.Identity;
using Microsoft.Azure.Cosmos;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services => {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddSingleton<CosmosClient>(serviceProvider =>
        {
            return new CosmosClient(
                Environment.GetEnvironmentVariable("CosmosDB__accountEndpoint"),
                new DefaultAzureCredential()
            );
        });

    })
    .Build();

host.Run();
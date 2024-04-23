using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Azure;
using Microsoft.Azure.Cosmos;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services => {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddSingleton<CosmosClient>(serviceProvider =>
        {
            return new CosmosClient(Environment.GetEnvironmentVariable("CosmosDbEndpointUrl"),Environment.GetEnvironmentVariable("CosmosDbPrimaryKey"));
        });

    })
    .Build();

host.Run();
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using System.Text.Json;
using Newtonsoft.Json.Linq;


namespace Modules.Modeling
{

    public class RequestValues
    {
        public int? Id { get; set; }
        public string? Title { get; set; }
        public string? ModelContainer { get; set; }
        public string? SearchType { get; set; }
    }
    public class ModelSingleDocument
    {
        public string id { get; set; }
        public string type { get; set; }
        public string title { get; set; }
        public string tagline { get; set; }
        public string description { get; set; }
        public string mpaa_rating { get; set; }
        public string release_date { get; set; }
        public string poster_url { get; set; }
        public Genre[] genres { get; set; }
        public Actor[] actors { get; set; }
        public Director[] directors { get; set; }
        public string _rid { get; set; }
        public string _self { get; set; }
        public string _etag { get; set; }
        public string _attachments { get; set; }
        public int _ts { get; set; }
    }

    public class Genre
    {
        public string name { get; set; }
        public string id { get; set; }
    }

    public class Actor
    {
        public string name { get; set; }
        public string id { get; set; }
    }

    public class Director
    {
        public string name { get; set; }
        public string id { get; set; }
    }
    public class CosmosDbService
    {
        public Container _container;

        public CosmosDbService(
            string endpointUrl,
            string primaryKey,
            string databaseName,
            string containerName)
        {
            CosmosClient client = new CosmosClient(endpointUrl, primaryKey);
            _container = client.GetContainer(databaseName, containerName);
        }
        // public class CosmosDbService
        // {
        //     public Container _container;

        //     public CosmosDbService(
        //         CosmosClient dbClient,
        //         string databaseName,
        //         string containerName)
        //     {
        //         _container = dbClient.GetContainer(databaseName, containerName);
        //     }

        public async Task<ItemResponse<dynamic>> GetItemAsync(string id, string title)
        {
            try
            {
                ItemResponse<dynamic> response = await _container.ReadItemAsync<dynamic>(id, new PartitionKey(title));
                return response;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }
        // }
    }

    public class QueryModel
    {
        private readonly ILogger<QueryModel> _logger;

        public QueryModel(ILogger<QueryModel> logger)
        {
            _logger = logger;
        }

        [Function("QueryModel")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "module/modeling")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            RequestValues requestValues = JsonSerializer.Deserialize<RequestValues>(requestBody);

            CosmosDbService cosmosDbService = new CosmosDbService(
                Environment.GetEnvironmentVariable("CosmosDbEndpointUrl"),
                Environment.GetEnvironmentVariable("CosmosDbPrimaryKey"),
                Environment.GetEnvironmentVariable("CosmosDbDatabaseName"),
                requestValues.ModelContainer
            );

            if (requestValues.Id != null && requestValues.Title != null)
            {

                return new BadRequestObjectResult("We would do a point read on id: " + requestValues.Id + " and a partition key " + requestValues.Title + "for model " + requestValues.ModelContainer);

            }
            else
            {
                // if (requestValues.SearchType == null)
                // {
                //     string sqlQueryText = $"SELECT * FROM c WHERE c.title = '{requestValues.Title}'";
                // }
                // else
                // {
                //     string sqlQueryText = $"SELECT * FROM c WHERE c.title = '{requestValues.Title}'";
                // }
                string sqlQueryText = $"SELECT * FROM c WHERE c.title = '{requestValues.Title}'";
                _logger.LogInformation("Executing query: {0}\n", sqlQueryText);
                var queryDefinition = new QueryDefinition(sqlQueryText);
                FeedIterator<dynamic> queryResultSetIterator = cosmosDbService._container.GetItemQueryIterator<dynamic>(queryDefinition);
                // Run the SQL query against Cosmos DB
                List<dynamic> results = new List<dynamic>();
                while (queryResultSetIterator.HasMoreResults)
                {
                    FeedResponse<dynamic> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    foreach (var item in currentResultSet)
                    {
                        JObject jObject = JObject.FromObject(item);
                        results.Add(jObject);
                        //results.Add(item);
                        Console.WriteLine($"Found item:\t{item}");
                    }
                }

                return new OkObjectResult(results);
            }

        }
    }
}
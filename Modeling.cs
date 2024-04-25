using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;


namespace Modules.Modeling
{

    public class RequestValues
    {
        public string SearchValue { get; set; }
        public string? SearchType { get; set; }
        public int? Id { get; set; }
    }

    public class QueryCosmos(ILogger<QueryCosmos> logger, CosmosClient cosmosClient)
    {
        private readonly ILogger<QueryCosmos> _logger = logger;
        private readonly CosmosClient _cosmosClient = cosmosClient;
        public string? SearchType { get; set; }

        [Function("QueryCosmos")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "module/modeling/{container}")] HttpRequest req, string container)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            RequestValues RequestValues = System.Text.Json.JsonSerializer.Deserialize<RequestValues>(requestBody);

            Container cosmosContainer  = _cosmosClient.GetContainer(Environment.GetEnvironmentVariable("CosmosDbDatabaseName"), container);

            QueryResult QueryResult = new QueryResult();

            RequestDiagnostics RequestDiagnostics = new RequestDiagnostics
            {
                // Adding request into to the diagnostic result
                SubmittedSearchValue = RequestValues.SearchValue,
                FormattedSearchValue = RequestValues.SearchValue.ToLower(), // Lowercase the search value for the query to match data title format
                Container = container
            };

            // If we have both Id and a SearchValue then we can attempt a Point Read.
            if (RequestValues.Id != null && RequestValues.SearchValue != null)
            {
                RequestDiagnostics.QueryType = "Point Read";

                //ItemResponse<Object> response = await cosmosContainer.ReadItemAsync<Object>(RequestValues.Id.ToString(), new PartitionKey(RequestValues.SearchValue));

            }
            else
            {
                RequestDiagnostics.QueryType = "SQL Query";
                // Check if this is a person query in single model as it has a unique query, otherwise standard title query
                if (RequestValues.SearchType == "person")
                {
                    RequestDiagnostics.QueryText = $"SELECT c.id, c.title, c.original_title, c.year, c.genres, c.actors, c.directors, c.type FROM c JOIN a IN c.actors JOIN d IN c.directors where a.name ='{RequestDiagnostics.FormattedSearchValue}' or d.name = '{RequestDiagnostics.FormattedSearchValue}'";
                }
                else
                {
                    RequestDiagnostics.QueryText = $"SELECT * FROM c WHERE c.title = '{RequestDiagnostics.FormattedSearchValue}'";
                }

                FeedIterator<Object> queryResultSetIterator = cosmosContainer.GetItemQueryIterator<Object>(RequestDiagnostics.QueryText);
                
                // Return a 404 if no results are found
                if (queryResultSetIterator.HasMoreResults == false)
                {
                    return new NotFoundResult();
                }

                while (queryResultSetIterator.HasMoreResults)
                {
                    //Check for the document type and deserialize it into the appropriate class
                    FeedResponse<Object> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    foreach (Object item in currentResultSet)
                    {
                        JObject jObject = JObject.FromObject(item);
                        string itemtype = jObject["type"].ToString();
                        string itemModel = "Media" + container + char.ToUpper(itemtype[0]) + itemtype.Substring(1);
                        _logger.LogInformation("Type: {0}", itemtype);
                        _logger.LogInformation("Model: {0}", itemModel);

                        object MediaItem;
                        switch (itemModel)
                        {
                            case "MediaEmbeddedPerson":
                                MediaItem = JsonConvert.DeserializeObject<MediaEmbeddedPerson>(jObject.ToString());
                                break;
                            case "MediaReferencePerson":
                                MediaItem = JsonConvert.DeserializeObject<MediaReferencePerson>(jObject.ToString());
                                break;
                            case "MediaHybridPerson":
                                MediaItem = JsonConvert.DeserializeObject<MediaHybridPerson>(jObject.ToString());
                                break;
                            default: // Default to single model for media as it can be used for all types in that container
                                MediaItem = JsonConvert.DeserializeObject<MediaSingle>(jObject.ToString());
                                break;
                        }

                        // _logger.LogInformation("Item is {0}", MediaItem);
                        // _logger.LogInformation("Item document type {0}", itemModel);

                        QueryResult.MediaResults.Add(MediaItem);

                    }

                    // Set some of the diagnostic values for the query
                    RequestDiagnostics.RequestCharge = currentResultSet.RequestCharge.ToString();
                    RequestDiagnostics.ActivityId = currentResultSet.ActivityId;
                }

                // TODO addd some kind of check for no results found
                // if (QueryResult.MediaResults.Count == 0)
                // {
                //     return new NotFoundResult();
                // }
                // _logger.LogInformation("Query Result: {0}", JsonConvert.SerializeObject(QueryResult));
                
                //Some reason I had to use JsonConvert.SerializeObject to get the result to return as a string, was OkObjectResult being Lazy?
            }
            QueryResult.RequestDiagnostics = RequestDiagnostics;
            return new OkObjectResult(JsonConvert.SerializeObject(QueryResult));
        }
    }

    public class MediaSingle
    {
        public string id { get; set; }
        public string type { get; set; }
        public string title { get; set; }
        public string original_title { get; set; }
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

    public class MediaEmbeddedPerson
    {
        public string id { get; set; }
        public string type { get; set; }
        public string title { get; set; }
        public string original_title { get; set; }
        public string movie_id { get; set; }
        public string movie_title { get; set; }
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

    public class MediaReferencePerson
    {
        public string id { get; set; }
        public string type { get; set; }
        public string title { get; set; }
        public string original_title { get; set; }
        public string movie_id { get; set; }
        public string movie_title { get; set; }
        public string tagline { get; set; }
        public string description { get; set; }
        public string mpaa_rating { get; set; }
        public string release_date { get; set; }
        public string poster_url { get; set; }
        public string _rid { get; set; }
        public string _self { get; set; }
        public string _etag { get; set; }
        public string _attachments { get; set; }
        public int _ts { get; set; }
    }

    public class MediaHybridPerson
    {
        public string id { get; set; }
        public string title { get; set; }
        public string original_title { get; set; }
        public string type { get; set; }
        public Role[] roles { get; set; }
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

    public class Role
    {
        public string movie_id { get; set; }
        public string movie_title { get; set; }
        public string mpaa_rating { get; set; }
        public string release_date { get; set; }
        public string role { get; set; }
    }

    public class QueryResult
    {
        public List<dynamic> MediaResults = new List<dynamic>();
        public RequestDiagnostics RequestDiagnostics = new RequestDiagnostics();
    }

    public class RequestDiagnostics
    {
        public string QueryText { get; set; }
        public string SubmittedSearchValue { get; set; }
        public string FormattedSearchValue { get; set; }
        public string RequestCharge { get; set; }
        public string ActivityId { get; set; }
        public string QueryType { get; set; }
        public string Container { get; set; }
    }
}
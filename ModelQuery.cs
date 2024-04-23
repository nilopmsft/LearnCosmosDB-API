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
        public int? Id { get; set; }
        public string? type { get; set; }
    }

    public class QueryCosmos
    {
        private readonly ILogger<QueryCosmos> _logger;
        private readonly CosmosClient _cosmosClient;
        public string? SearchType { get; set; }

        public QueryCosmos(ILogger<QueryCosmos> logger, CosmosClient cosmosClient)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
        }


        [Function("QueryCosmos")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "module/modeling/{container}")] HttpRequest req, string container)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            RequestValues RequestValues = System.Text.Json.JsonSerializer.Deserialize<RequestValues>(requestBody);

            Container cosmosContainer  = _cosmosClient.GetContainer(Environment.GetEnvironmentVariable("CosmosDbDatabaseName"), container);

            // if (requestValues.Id != null && requestValues.Title != null)
            // {

            //     return new BadRequestObjectResult("We would do a point read on id: " + requestValues.Id + " and a partition key " + requestValues.Title + "for model " + requestValues.ModelContainer);

            // }
            // else
            // {
                // if (requestValues.SearchType == null)
                // {
                //     string sqlQueryText = $"SELECT * FROM c WHERE c.title = '{requestValues.Title}'";
                // }
                // else
                // {
                //     string sqlQueryText = $"SELECT * FROM c WHERE c.title = '{requestValues.Title}'";
                // }
                string SearchValue = RequestValues.SearchValue.ToLower();
                string sqlQueryText = $"SELECT * FROM c WHERE c.title = '{SearchValue}'";
                // _logger.LogInformation("Executing query: {0}\n", sqlQueryText);

                FeedIterator<Object> queryResultSetIterator = cosmosContainer.GetItemQueryIterator<Object>(sqlQueryText);
                QueryResult QueryResult = new QueryResult();

                // Adding request into to the diagnostic result
                QueryResult.QueryDiagnostics.QueryText = sqlQueryText;
                QueryResult.QueryDiagnostics.SearchValue = SearchValue;
                

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

                        _logger.LogInformation("Item is {0}", MediaItem);
                        _logger.LogInformation("Item document type {0}", itemModel);

                        QueryResult.MediaResults.Add(MediaItem);

                    }

                    QueryResult.QueryDiagnostics.RequestCharge = currentResultSet.RequestCharge.ToString();
                    QueryResult.QueryDiagnostics.ActivityId = currentResultSet.ActivityId;
                }
                _logger.LogInformation("Query Result: {0}", QueryResult);
                
                return new OkObjectResult(QueryResult);
            }
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
        public QueryDiagnostics QueryDiagnostics = new QueryDiagnostics();
    }

    public class QueryDiagnostics
    {
        public string QueryText { get; set; }
        public string SearchValue { get; set; }
        public string RequestCharge { get; set; }
        public string ActivityId { get; set; }
    }


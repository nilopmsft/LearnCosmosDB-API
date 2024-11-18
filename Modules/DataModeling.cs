using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;


namespace Modules.Modeling
{

    public class DataModeling(ILogger<DataModeling> logger, CosmosClient cosmosClient)
    {
        private readonly ILogger<DataModeling> _logger = logger;
        private readonly CosmosClient _cosmosClient = cosmosClient;
        public string? SearchType { get; set; }

        [Function("DataModeling")]
        // public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", Route = "module/DataModeling/{container}")] HttpRequest req, string container)
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {

            if (string.IsNullOrEmpty(req.Query["searchValue"].ToString()))
            {
                return new BadRequestObjectResult("Please provide a valid data model: 'Single','Embedded','Reference','Hybrid'");
            }
            if (string.IsNullOrEmpty(req.Query["searchValue"].ToString()))
            {
                return new BadRequestObjectResult("Please provide a valid search value");
            }

            // Create class of query values sent in url to RequestValues, leave null if not present
            RequestValues RequestValues = new RequestValues
            {
                dataModel = !string.IsNullOrEmpty(req.Query["dataModel"]) ? req.Query["dataModel"].ToString() : null,
                searchValue = !string.IsNullOrEmpty(req.Query["searchValue"]) ? req.Query["searchValue"].ToString() : null,
                searchType = !string.IsNullOrEmpty(req.Query["searchType"]) ? req.Query["searchType"].ToString() : null,
                docId = !string.IsNullOrEmpty(req.Query["docId"]) ? req.Query["docId"].ToString() : null
            };

            // Create class to hold the diagnostics and metadata of the request.
            RequestDiagnostics RequestDiagnostics = new RequestDiagnostics
            {
                DataModel = RequestValues.dataModel,
                SubmittedSearchValue = RequestValues.searchValue,
                FormattedSearchValue = RequestValues.searchValue.ToLower(), // Lowercase the search value for the query to match data title format, This should be something we change to computed later
            };

            string container = RequestValues.dataModel;

            Container cosmosContainer = _cosmosClient.GetContainer("MediaModeling", container);

            QueryResult QueryResult = new QueryResult();

            // If we have both Id and a SearchValue then we can utilize a Point Read. Search Value assumes a passed in partition key value to match its documentId.
            // You realistically wouldnt get here if you didnt know what the id and search value that match for a document, i.e. programmatically.
            if (RequestValues.docId != null && RequestValues.searchValue != null)
            {
                RequestDiagnostics.QueryType = "Point Read";
                RequestDiagnostics.DocId = RequestValues.docId;
                try
                {
                    ItemResponse<Object> response = await cosmosContainer.ReadItemAsync<Object>(RequestValues.docId, new PartitionKey(RequestValues.searchValue.ToLower()));
                    object Item = ResultModeling.GetItemModel(RequestValues.dataModel, response.Resource);
                    QueryResult.MediaResults.Add(Item);

                    // Set activity ID and request charge for the query.
                    RequestDiagnostics.RequestCharge = response.RequestCharge.ToString();
                    RequestDiagnostics.ActivityId = response.ActivityId;
                }
                catch (CosmosException ex)
                {
                    if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        throw new Exception("Document not found", ex);
                    }
                    else
                    {
                        throw new Exception("An error occurred", ex);
                    }
                }
            }
            else
            {
                // Query Type is a SQL Query, not point read
                RequestDiagnostics.QueryType = "SQL Query";

                // If we have a type of person then we know its a 'single' model lookup since that is the only container documents that have embedded person's that are its not partitioned by
                if (RequestValues.searchType == "person" && RequestValues.dataModel == "Single")
                {
                    RequestDiagnostics.QueryText = $"SELECT c.id, c.title, c.original_title, c.year, c.genres, c.actors, c.directors, c.type FROM c JOIN a IN c.actors JOIN d IN c.directors where a.name ='{RequestValues.searchValue.ToLower()}' or d.name = '{RequestValues.searchValue.ToLower()}'";
                }
                else
                {
                    //The container has documents that represent movies or people as their partition key
                    RequestDiagnostics.QueryText = $"SELECT * FROM c WHERE c.title = '{RequestValues.searchValue.ToLower()}'";
                }

                _logger.LogInformation("Query Text: {0}", RequestDiagnostics.QueryText);

                try
                {
                    FeedIterator<Object> queryResultSetIterator = cosmosContainer.GetItemQueryIterator<Object>(RequestDiagnostics.QueryText);

                    while (queryResultSetIterator.HasMoreResults)
                    {
                        //Check for the document type and deserialize it into the appropriate class
                        FeedResponse<Object> currentResultSet = await queryResultSetIterator.ReadNextAsync();

                        // Return a 404 if no results are found
                        if (currentResultSet.Count == 0)
                        {
                            _logger.LogInformation("No results found for query");
                            return new NotFoundResult();
                        }
                        else
                        {
                            foreach (Object item in currentResultSet)
                            {
                                object Item = ResultModeling.GetItemModel(container, item);
                                QueryResult.MediaResults.Add(Item);

                            }

                            // Set some of the diagnostic values for the query
                            RequestDiagnostics.RequestCharge = currentResultSet.RequestCharge.ToString();
                            RequestDiagnostics.ActivityId = currentResultSet.ActivityId;
                        }
                    }
                }
                catch (CosmosException ex)
                {
                    if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        throw new Exception("Document not found", ex);
                    }
                    else
                    {
                        throw new Exception("An error occurred", ex);
                    }
                }
            }

            QueryResult.RequestDiagnostics = RequestDiagnostics;

            //Some reason I had to use JsonConvert.SerializeObject to get the result to return as a string, was OkObjectResult being Lazy?
            return new OkObjectResult(JsonConvert.SerializeObject(QueryResult));
        }

        public class RequestValues
        {
            public string? dataModel { get; set; }
            public string? searchValue { get; set; }
            public string? searchType { get; set; }
            public string? docId { get; set; }
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
            public string DataModel { get; set; }
            public string QueryText { get; set; }
            public string SubmittedSearchValue { get; set; }
            public string FormattedSearchValue { get; set; }
            public string DocId { get; set; }
            public string RequestCharge { get; set; }
            public string ActivityId { get; set; }
            public string QueryType { get; set; }
            public string Container { get; set; }
        }

        public class ResultModeling
        {
            public static object GetItemModel(string container, object item)
            {

                JObject jObject = JObject.FromObject(item);
                string itemtype = jObject["type"].ToString();
                string itemModel = "Media" + container + char.ToUpper(itemtype[0]) + itemtype.Substring(1);

                object Item;
                switch (itemModel)
                {
                    case "MediaEmbeddedPerson":
                        Item = JsonConvert.DeserializeObject<MediaEmbeddedPerson>(jObject.ToString());
                        break;
                    case "MediaReferencePerson":
                        Item = JsonConvert.DeserializeObject<MediaReferencePerson>(jObject.ToString());
                        break;
                    case "MediaHybridPerson":
                        Item = JsonConvert.DeserializeObject<MediaHybridPerson>(jObject.ToString());
                        break;
                    default: // Default to single model for media as it can be used for all types in that container
                        Item = JsonConvert.DeserializeObject<MediaSingle>(jObject.ToString());
                        break;
                }
                return Item;
            }
        }
    }
}
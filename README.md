WIP

# Summary

This repo holds the backend API for the [Learn Cosmos DB](https://learncosmosdb.com) web site.

## Modules

Below are the various modules within the API and their specifications for use.

- [Data Modeling](#data-modeling): Demonstration of how modeling data stored in documents for the application/client use cases can demonstrate major improvements on performance, storage and manageability.

## API

All API requests will require a header value of 'x-functions-key' with the functions App Key designated for us (typically 'Default')

### Data Modeling

**Endpoint** : `/api/DataModeling/` for all related requests

**Parameters** to be provided are:

- `dataModel` (required): This represents which 'model' we are using for our search. Determines which container to query in our Cosmos Database. Options, 'Single','Embedded','Reference','Hybrid'

- `searchValue` (required): Actual search value passed by user/application. Both user input queries and point reads requires this value.

- `searchType` (optional): type of search, 'movie', 'person' or 'point'. This due to the variety of models we have and is not always required. See respective request information.

- `docId` (optional): Id of document if we have it available. Used for Point Reads.

**Documents** will be two dictionaries of MediaResults and RequestDiagnostics. Review the below query and model chosen outputs to see respective structure.

**Movie Query**

Retrieve movies by search term. Regrdless of model, each movie has a single document for its individual movie which is retrieved by the searchValue if matching.

- **GET** `/api/DataModeling/`
- **Parameters:**
  - `dataModel`: `Single`
  - `searchValue`: `<User input>`

- Example Request:

`GET <function_host>/api/DataModeling/?dataModel=Single&searchValue=<search Input>&searchType=movie`

- Example Response:

```
{
  "MediaResults": [
    {
      "id": "00f338d70bab89f1",
      "type": "movie",
      "title": "eclipse of eons",
      "original_title": "Eclipse of Eons",
      "tagline": "In a time forgotten, the fight for justice knows no bounds.",
      "description": "In 'Eclipse of Eons', an ancient world cloaked in mystery becomes the battleground for a timeless struggle. Alfred Moussakalina stars as a cynical detective endowed with the wisdom of the ages, while Robin Downey Jr. embodies a stealthy ninja whose martial prowess is unmatched. J.K. Simmrolls completes the trio as a sage samurai tasked with upholding honor in an era rife with prejudice and injustice. Directed by the visionary Sidney Lumoose, this Sci-Fi epic weaves fantasy origins with a tapestry of betrayal and redemption. As these warriors from different walks of life unravel a conspiracy that threatens the fabric of their society, they must confront their own prejudices to restore balance to their world. The past may be written, but the future is theirs to define.",
      "mpaa_rating": "PG-13",
      "release_date": "2021-08-05",
      "poster_url": "https://battlecabbagemedia.blob.core.windows.net/movies/initial_load/images/00f338d70bab89f1.jpg?sp=r&st=2024-08-02T21:43:32Z&se=2035-08-03T05:43:32Z&spr=https&sv=2022-11-02&sr=c&sig=4nrwhGwOK7OTVRelzDySkuuTprGwTAp1YO3dBlmBijw%3D",
      "genres": [
        {
          "name": "Sci-Fi",
          "id": "gen17"
        }
      ],
      "actors": [
        {
          "name": "alfred moussakalina",
          "id": "act19"
        },
        {
          "name": "j.k. simmrolls",
          "id": "act438"
        },
        {
          "name": "robin downey jr.",
          "id": "act867"
        }
      ],
      "directors": [
        {
          "name": "sidney lumoose",
          "id": "dir63"
        }
      ],
      "_rid": "QQNvAIGjR0gGAAAAAAAAAA==",
      "_self": "dbs/QQNvAA==/colls/QQNvAIGjR0g=/docs/QQNvAIGjR0gGAAAAAAAAAA==/",
      "_etag": "\"f300085c-0000-0200-0000-66c38cb40000\"",
      "_attachments": "attachments/",
      "_ts": 1724091572
    }
  ],
  "RequestDiagnostics": {
    "DataModel": "Single",
    "QueryText": "SELECT * FROM c WHERE c.title = 'eclipse of eons'",
    "SubmittedSearchValue": "Eclipse of Eons",
    "FormattedSearchValue": "eclipse of eons",
    "DocId": null,
    "RequestCharge": "2.85",
    "ActivityId": "1ac92d2f-918f-4a36-8b76-3935adaae3b3",
    "QueryType": "SQL Query",
    "Container": null
  }
}
```

**Person Query**

Query for finding documents by person name, be it director or actor. dataModel of Single requires the searchValue is 'person'  which dictates the backend query used. Making searchValue of 'person' critical here. All other dataModels ignore the 'person' search type and thus not required but for sanity should be included.


- **Parameters**:

    - `dataModel` : `<Any Model>` *This is dictated by the UI as to which model we are querying*

    - `searchValue` : `<User input>`

    - `searchType` : `person` *Required for Single model searches*

- Example Single Model Person

`GET <function_host>/api/DataModeling/?dataModel=Single&searchValue=<search Input>&searchType=person`

```
{
  "MediaResults": [
    {
      "id": "104c09456043c64a",
      "type": "movie",
      "title": "mask of the modern mage",
      "original_title": "Mask of the Modern Mage",
      "tagline": null,
      "description": null,
      "mpaa_rating": null,
      "release_date": null,
      "poster_url": null,
      "genres": [
        {
          "name": "Supernatural",
          "id": "gen20"
        }
      ],
      "actors": [
        {
          "name": "ant millet",
          "id": "act52"
        },
        {
          "name": "eel-thel berry-more",
          "id": "act295"
        },
        {
          "name": "michael douglaze",
          "id": "act731"
        }
      ],
      "directors": [
        {
          "name": "andrei tarkovskunk",
          "id": "dir5"
        }
      ],
      "_rid": null,
      "_self": null,
      "_etag": null,
      "_attachments": null,
      "_ts": 0
    },
    <...More Results...>
  ],
  "RequestDiagnostics": {
    "DataModel": "Single",
    "QueryText": "SELECT c.id, c.title, c.original_title, c.year, c.genres, c.actors, c.directors, c.type FROM c JOIN a IN c.actors JOIN d IN c.directors where a.name ='andrei tarkovskunk' or d.name = 'andrei tarkovskunk'",
    "SubmittedSearchValue": "Andrei Tarkovskunk",
    "FormattedSearchValue": "andrei tarkovskunk",
    "DocId": null,
    "RequestCharge": "5.69",
    "ActivityId": "3d88bd8d-19d2-426d-8900-e9a29bc70dd8",
    "QueryType": "SQL Query",
    "Container": null
  }
}
```

- Example Embedded Model Person

`GET <function_host>/api/DataModeling/?dataModel=Embedded&searchValue=<search Input>&searchType=person`

```
{
  "MediaResults": [
    {
      "id": "mov104c09456043c64adir5",
      "type": "person",
      "title": "andrei tarkovskunk",
      "original_title": "Andrei Tarkovskunk",
      "movie_id": "104c09456043c64a",
      "movie_title": "Mask of the Modern Mage",
      "tagline": "Some transformations are more than magical, they're sarcastic.",
      "description": "In a world where the supernatural is just another Tuesday, Eel-thel Berry-more stars as a superhero with a twist. Eel-thel is an academic with a knack for ancient lore who stumbles upon a mystical relic that binds with her very essence, throwing her into a reality where her research on European identity and transformation isn't just theoretical anymore. Alongside fellow academic Ant Millet and skeptical debunker Michael Douglaze, Eel-thel masterfully juggles the dual life of a bookish historian and a witty crime-fighting enchantress. But as the forces of the arcane grow stronger, our heroes must ask themselves—do they study history, or do they make it? Directed by the offbeat visionary Andrei Tarkovskunk, 'Mask of the Modern Mage' casts a whimsical spell of irony and charm in this supernatural romp through the contemporary era.",
      "mpaa_rating": "PG",
      "release_date": "2021-10-07",
      "poster_url": "https://battlecabbagemedia.blob.core.windows.net/movies/initial_load/images/104c09456043c64a.jpg?sp=r&st=2024-08-02T21:43:32Z&se=2035-08-03T05:43:32Z&spr=https&sv=2022-11-02&sr=c&sig=4nrwhGwOK7OTVRelzDySkuuTprGwTAp1YO3dBlmBijw%3D",
      "genres": [
        {
          "name": "Supernatural",
          "id": "gen20"
        }
      ],
      "actors": [
        {
          "name": "ant millet",
          "id": "act52"
        },
        {
          "name": "eel-thel berry-more",
          "id": "act295"
        },
        {
          "name": "michael douglaze",
          "id": "act731"
        }
      ],
      "directors": [
        {
          "name": "andrei tarkovskunk",
          "id": "dir5"
        }
      ],
      "_rid": "QQNvALF1K7f4FQAAAAAAAA==",
      "_self": "dbs/QQNvAA==/colls/QQNvALF1K7c=/docs/QQNvALF1K7f4FQAAAAAAAA==/",
      "_etag": "\"f3001083-0000-0200-0000-66c38f3f0000\"",
      "_attachments": "attachments/",
      "_ts": 1724092223
    },
    <...More Results...>
  ],
  "RequestDiagnostics": {
    "DataModel": "Embedded",
    "QueryText": "SELECT * FROM c WHERE c.title = 'andrei tarkovskunk'",
    "SubmittedSearchValue": "Andrei Tarkovskunk",
    "FormattedSearchValue": "andrei tarkovskunk",
    "DocId": null,
    "RequestCharge": "4.49",
    "ActivityId": "8102a27b-5378-446e-80eb-8c2f3299dd6e",
    "QueryType": "SQL Query",
    "Container": null
  }
}
```

- Example Reference Model Person

`GET <function_host>/api/DataModeling/?dataModel=Reference&searchValue=<search Input>&searchType=person`


```
{
  "MediaResults": [
    {
      "id": "mov104c09456043c64adir5",
      "type": "person",
      "title": "andrei tarkovskunk",
      "original_title": "Andrei Tarkovskunk",
      "movie_id": "104c09456043c64a",
      "movie_title": "Mask of the Modern Mage",
      "tagline": null,
      "description": null,
      "mpaa_rating": "PG",
      "release_date": "2021-10-07",
      "poster_url": "https://battlecabbagemedia.blob.core.windows.net/movies/initial_load/images/104c09456043c64a.jpg?sp=r&st=2024-08-02T21:43:32Z&se=2035-08-03T05:43:32Z&spr=https&sv=2022-11-02&sr=c&sig=4nrwhGwOK7OTVRelzDySkuuTprGwTAp1YO3dBlmBijw%3D",
      "_rid": "QQNvAKNCFqX4FQAAAAAAAA==",
      "_self": "dbs/QQNvAA==/colls/QQNvAKNCFqU=/docs/QQNvAKNCFqX4FQAAAAAAAA==/",
      "_etag": "\"f3003a99-0000-0200-0000-66c390890000\"",
      "_attachments": "attachments/",
      "_ts": 1724092553
    },
    <...More Results...>
  ],
  "RequestDiagnostics": {
    "DataModel": "Reference",
    "QueryText": "SELECT * FROM c WHERE c.title = 'andrei tarkovskunk'",
    "SubmittedSearchValue": "Andrei Tarkovskunk",
    "FormattedSearchValue": "andrei tarkovskunk",
    "DocId": null,
    "RequestCharge": "3.73",
    "ActivityId": "22405b10-9694-4861-b2db-1b9b1348ed67",
    "QueryType": "SQL Query",
    "Container": null
  }
}
```

- Example Hybrid Model Person

`GET <function_host>/api/DataModeling/?dataModel=Hybrid&searchValue=<search Input>&searchType=person`

```
{
  "MediaResults": [
    {
      "id": "dir5",
      "title": "andrei tarkovskunk",
      "original_title": "Andrei Tarkovskunk",
      "type": "person",
      "roles": [
        {
          "movie_id": "104c09456043c64a",
          "movie_title": "Mask of the Modern Mage",
          "mpaa_rating": "PG",
          "release_date": "2021-10-07",
          "role": "director"
        },
        <...More Results...>
      ],
      "_rid": "QQNvAJYeo3quCQAAAAAAAA==",
      "_self": "dbs/QQNvAA==/colls/QQNvAJYeo3o=/docs/QQNvAJYeo3quCQAAAAAAAA==/",
      "_etag": "\"f300cda2-0000-0200-0000-66c391310000\"",
      "_attachments": "attachments/",
      "_ts": 1724092721
    }
  ],
  "RequestDiagnostics": {
    "DataModel": "Hybrid",
    "QueryText": "SELECT * FROM c WHERE c.title = 'andrei tarkovskunk'",
    "SubmittedSearchValue": "Andrei Tarkovskunk",
    "FormattedSearchValue": "andrei tarkovskunk",
    "DocId": null,
    "RequestCharge": "2.89",
    "ActivityId": "f5bd468f-a0b9-4266-aa92-d8333e2e3fc3",
    "QueryType": "SQL Query",
    "Container": null
  }
}
```

**Point Read**

Scenarios where we have both the partition value (title) and the document ID (id), actions that are retrieving a document, e.g. clicking on a movie for its details, allows us to utilize a point read, the most effective way of retrieving individual documents.

- **GET**  `/api/DataModeling/`
- **Parameters:**
  - `searchValue`: `<User input>`
  - `docId`: `<Document ID>`

- Example Movie Point Read

`GET <function_host>/api/DataModeling/?searchValue=<search Input>&docId=<docId>`

Results would be the same as returning a single movie, primary difference being the query cost being fundamentally cheaper and results limited to the single document requested (i.e. a movie with multiple of the same name would be multiple documents which is not a point read, that would be retrieving a specific version of that movie title)

## Utilities

WORK IN PROGRESS. But anticipated tools such as gathering status of API and cosmos backend. Larger metrics like size of containers, indexes and RU consumption.

## Configuration

If looking to deploy this API, there are some recommended configurations to include.

### AZ CLI Commands

Run the below commands with the proper values as they will be used through the rest of the documented CLI commands.

```
cosmos_account=<Cosmos-Account> 
resource_group=<Cosmos-Account-ResourceGroup>
```

- Get ID of the cosmos account, commonly used.

```
cosmos_account_id=$(az cosmosdb show --resource-group $resource_group --name $cosmos_account --query "id" -o tsv)
echo $cosmos_account_id
```

#### View Resource

Review the current configuration of the Azure Cosmos DB Account

`az cosmosdb  show --name $cosmos_account --resource-group $resource_group`

#### Create Custom RBAC Role

[This link](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/security/how-to-grant-data-plane-role-based-access?tabs=built-in-definition%2Ccsharp&pivots=azure-interface-cli#permission-model) provides the instructions for creating the proper RBAC permissions via CLI. This is relatively simple and hopefully will be provided in the portal in the future.

#### Configure Local Auth

- Check Local Auth Status

`az cosmosdb show --resource-group $resource_group --name $cosmos_account --query "{disableLocalAuth:disableLocalAuth}"`

- Get ID

```
cosmos_account_id=$(az cosmosdb show --resource-group $resource_group --name $cosmos_account --query "id" -o tsv)
echo $cosmos_account_id
```

- Update Local auth with either 'true' or 'false'.

`az resource update --ids $cosmos_account_id --set properties.disableLocalAuth=false`
// See https://aka.ms/new-console-template for more information

using NRedisStack.Search;
using OpenAI;
using StackExchange.Redis;
using System.Text;
using System.Linq;

Console.WriteLine("Hello, World!");
var auth = OpenAIAuthentication.LoadFromDirectory();

var openAIClient = new OpenAIClient(auth);

var result = await openAIClient.EmbeddingsEndpoint.CreateEmbeddingAsync("aaa");
var vector = result
    .Data[0]
    .Embedding
    .Select
        (
            (x) =>
            {
                return (float)x;
            }
        )
    .ToArray();

var redis = ConnectionMultiplexer.Connect("dev-001.eastasia.cloudapp.azure.com,password=p@$$w0rdw!th0ut");
var db = redis.GetDatabase();

// Call the SearchRedis method
var results = SearchRedis(
    db,
    "query",
    vector,
    indexName: "embeddings-index",
    vectorField: "title_vector",
    
    hybridFields: "*",
    k: 20,
    printResults: true
);

// Do something with the results
foreach (var result1 in results)
{
    Console.WriteLine(result1["title"]);
}



List<Dictionary<string, object>> SearchRedis(
    IDatabase redisClient,
    string userQuery,
    float[] vector,
    string indexName = "embeddings-index",
    string vectorField = "title_vector",
    
    string hybridFields = "*",
    int k = 20,
    bool printResults = true
)
{
    string[] returnFields = new[] { "title", "url", "text", "vector_score" };
    // Creates embedding vector from user query
    //var embeddedQuery = OpenAI.Embedding.Create(userQuery, "text-embedding-ada-002")
    //    .Data[0].Embedding;

    // Prepare the Query
    var baseQuery = $"{hybridFields}=>[KNN {k} @{vectorField} $vector AS vector_score]";
    var query = new Query(baseQuery)
        .ReturnFields(returnFields)
        .SetSortBy("vector_score")
        .Limit(0, k)
        .Dialect(2);

    var paramsDict = new Dictionary<string, object> {
        { "vector", vector }
    };


    //RedisResult result = db.Execute("FT.SEARCH", "myIndex", query, options);
    var options = new RedisValue[]
    {
        "LIMIT", "0", "10", "WITHSCORES"
    };

    // perform vector search
    var results = (RedisResult[]) redisClient
                            .Execute
                                (
                                    "FT.SEARCH"
                                    , indexName
                                    , query.ToString()!
                                    , paramsDict
                                    , options
                                )!;
    var resultList =
        results
        .OfType<RedisResult[]>()
        .Select(x => returnFields.ToDictionary(
            field => field,
            field => (object)(x[Array.IndexOf(returnFields, field)].ToString())))
        .ToList();

    if (printResults)
    {
        for (int i = 0; i < resultList.Count; i++)
        {
            var article = resultList[i];
            var score = 1 - float.Parse(article["vector_score"].ToString());
            Console.WriteLine($"{i}. {article["title"]} (Score: {score:F3})");
        }
    }

    return resultList;
}

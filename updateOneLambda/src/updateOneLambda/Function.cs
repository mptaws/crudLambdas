using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace dnCrud
{
    public class Function
    {
        public class StudentRecords
        {
            [BsonId]
            [BsonRepresentation(BsonType.ObjectId)]
            [BsonIgnoreIfDefault]
            public ObjectId _id { get; set; }
            public string student_id { get; set; }
            public string type { get; set; }
            public string score { get; set; }
        }

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            var client = MongoConnect.Connect();
            var database = client.GetDatabase("test");
            var collection = database.GetCollection<StudentRecords>("collection");

            string id = apigProxyEvent.PathParameters["id"];
            // var filter = Builders<StudentRecords>.Filter.Eq("student_id", id);
            StudentRecords updateDocument = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<StudentRecords>(apigProxyEvent.Body);
            // var result = await collection.ReplaceOneAsync(filter, updateDocument, new ReplaceOptions { IsUpsert = true });

            var filter = Builders<StudentRecords>.Filter.Where(x => x.student_id == id);
            var options = new FindOneAndReplaceOptions<StudentRecords, StudentRecords>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };
            var result = await collection.FindOneAndReplaceAsync(filter, updateDocument, options);

            return new APIGatewayProxyResponse
            {
                Body = System.Text.Json.JsonSerializer.Serialize(result),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }

    }
}

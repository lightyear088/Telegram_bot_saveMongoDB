using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;


namespace Telegram.Bot.Temp
{
    public class Tempi
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Status { get; set; }
        public int Step { get; set; }

      
    }
}
/*
async static Task CreateBd(string data, string celebration, string nickname)
{
    var pr = new Program();
    string connectionString = "mongodb://localhost:27017";
    string dataBaSeName = "simple_db";
    string collectionName = "plans";


    var client = new MongoClient(connectionString);
    var db = client.GetDatabase(dataBaSeName);
    var collection = db.GetCollection<Event>(collectionName);
    var info = new Event { EventName = celebration, EventData = data, NickName = nickname };

    await collection.InsertOneAsync(info);
}
*/
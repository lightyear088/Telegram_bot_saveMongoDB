using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;


namespace Telegram.Bot.Examples.WebHook.Services
{
    public class BaseMongo
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string NickName { get; set; }
        public string EventName { get; set; }
        public string EventData { get; set; }
        public string QguestList { get; set; }
    }

}


using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;
using System;

namespace Telegram.Bot.Examples.WebHook.BD;

public class MongoDBTemp
{
    [BsonId]
    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    public string Id { get; set; }
    public long ChatId { get; set; }
    public string NickName { get; set; }
    public string Status { get; set; }
    public int Step { get; set; }
    //public List<string> ActiveCreation { get; set; } = new List<string>();

}




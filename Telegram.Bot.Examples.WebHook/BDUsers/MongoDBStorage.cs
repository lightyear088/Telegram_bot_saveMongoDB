using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Telegram.Bot.Examples.WebHook.BDUsers;

public class MongoDBStorage
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    public string Owner { get; set; }
    public bool Number { get; set; }
    public string NickName { get; set; }
    public string Name { get; set; }
    public DateTime Date { get; set; }
    public List<UserInfo> Guests{ get; set; } = new List<UserInfo>();
    public string Description { get; set; }
}

public class UserInfo
{
    public string ForeName { get; set; }
    public string Agreement { get; set; }
}

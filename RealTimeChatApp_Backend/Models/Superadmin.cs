using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDbGenericRepository.Attributes;

namespace RealTimeChatApp.API.Models
{
    [CollectionName("superadmins")]
    public class Superadmin
    {
        [BsonId]  
        [BsonRepresentation(BsonType.String)] // auto-convert objectId-string
        public string UserId {  get; set; }
        public string UserName { get; set; }
        public List<ObjectId> BroadcastedIds { get; set; }  // store the IDs of broadcasted messages
    }
}

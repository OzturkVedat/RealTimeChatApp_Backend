using AspNetCore.Identity.MongoDbCore.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDbGenericRepository.Attributes;

namespace RealTimeChatApp.API.Models
{
    [CollectionName("users")]
    public class ApplicationUser : MongoIdentityUser<string>
    {
        public string FullName { get; set; } = string.Empty;
        [BsonIgnoreIfNull]
        public RefreshToken RefreshToken { get; set; }
    }
}

using AspNetCore.Identity.MongoDbCore.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDbGenericRepository.Attributes;

namespace RealTimeChatApp.API.Models
{
    [CollectionName("users")]
    [BsonIgnoreExtraElements]
    public class UserModel : MongoIdentityUser<string>
    {
        public string FullName { get; set; } = string.Empty;
        [BsonIgnoreIfNull]
        public string? ProfileImageUrl { get; set; }  

        [BsonIgnoreIfNull]
        public string? StatusMessage { get; set; }    // Custom status message (e.g., "Available", "Busy")

        public List<GroupModel> Groups { get; set; }= new List<GroupModel>();
        [BsonIgnoreIfNull]
        public RefreshToken RefreshToken { get; set; }

    }
    public class RefreshToken
    {
        public string Token { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsRevoked { get; set; }
    }
}

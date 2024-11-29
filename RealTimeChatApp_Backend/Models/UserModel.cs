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
        public string FullName { get; set; }
        public string StatusMessage { get; set; }
        public string ProfilePictureUrl {  get; set; }
        public List<ObjectId> ChatIds { get; set; } = [];
        public List<ObjectId> GroupIds { get; set; } = [];
        public List<string> FriendsListIds { get; set; } = [];
        public bool IsOnline { get; set; }
        public RefreshToken? RefreshToken { get; set; }

    }
    public class RefreshToken
    {
        public string Token { get; set; } = Guid.NewGuid().ToString();
        public DateTime ExpiryDate { get; set; } = DateTime.UtcNow.AddDays(7);  // for one week use
        public bool IsRevoked { get; set; } = false;
    }
}

using MongoDbGenericRepository.Attributes;

namespace RealTimeChatApp.API.Models
{
    [CollectionName("tokens")]
    public class RefreshToken
    {
        public string Token { get; set; } 
        public DateTime ExpiryDate { get; set; } // Expiration date of the token
        public bool IsRevoked { get; set; } // Token revocation status
    }
}

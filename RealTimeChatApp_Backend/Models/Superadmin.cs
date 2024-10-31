using MongoDbGenericRepository.Attributes;

namespace RealTimeChatApp.API.Models
{
    [CollectionName("superadmins")]
    public class Superadmin
    {
        public string UserId {  get; set; }
        public string UserName { get; set; }
    }
}

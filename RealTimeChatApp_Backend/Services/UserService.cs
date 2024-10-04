using MongoDB.Driver;
using RealTimeChatApp.API.Models;

namespace RealTimeChatApp.API.Services
{
    public class UserService
    {
        private readonly IMongoCollection<UserModel> _usersCollection;

        public UserService(IMongoDatabase mongoDb)
        {
            _usersCollection = mongoDb.GetCollection<UserModel>("users");
        }


        public async Task<List<GroupModel>> GetUserJoinedGroups(string userId)
        {
            var user= await _usersCollection.Find(u=> u.Id==userId).FirstOrDefaultAsync();
            return user.Groups ?? new List<GroupModel>();
        }
    }
}

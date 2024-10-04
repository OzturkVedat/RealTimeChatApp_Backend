using MongoDB.Bson;
using MongoDB.Driver;
using RealTimeChatApp.API.Models;

namespace RealTimeChatApp.API.Services
{
    public class GroupService
    {
        private readonly IMongoCollection<GroupModel> _groupsCollection;

        public GroupService(IMongoDatabase mongoDb)
        {
            _groupsCollection = mongoDb.GetCollection<GroupModel>("groups");
        }

        public async Task<List<string>> GetUserIdsInGroupAsync(string groupId)
        {
            // Convert groupId from string to ObjectId
            if (!ObjectId.TryParse(groupId, out ObjectId groupObjectId))
            {
                return new List<string>(); // Invalid group ID format
            }

            var group = await _groupsCollection.Find(g => g.Id == groupObjectId).FirstOrDefaultAsync();
            return group.UserIds ?? new List<string>(); // Return the UserIds, or an empty list if null
        }

        
        public async Task<List<MessageModel>> GetGroupMessageHistory(string groupId)
        {

            if (!ObjectId.TryParse(groupId, out ObjectId groupObjectId))
                return new List<MessageModel>();

            var group = await _groupsCollection
                .Find(g => g.Id == groupObjectId)
                .FirstOrDefaultAsync();
            return group.Messages ?? new List<MessageModel>();
        }
    }
}

using MongoDB.Bson;
using MongoDB.Driver;
using RealTimeChatApp.API.Models;

namespace RealTimeChatApp.API.Services
{
    public class HubHelperService
    {
        private readonly IMongoCollection<GroupModel> _groupsCollection;

        public HubHelperService(IMongoDatabase mongoDb)
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
            if (group == null)
            {
                return new List<string>(); // Group not found
            }

            return group.UserIds ?? new List<string>(); // Return the UserIds, or an empty list if null
        }
    }
}

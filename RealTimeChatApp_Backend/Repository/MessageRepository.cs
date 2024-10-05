using MongoDB.Bson;
using MongoDB.Driver;
using RealTimeChatApp.API.Interface;
using RealTimeChatApp.API.Models;

namespace RealTimeChatApp.API.Repository
{

    public class MessageRepository : IMessageRepository
    {
        private readonly IMongoCollection<PrivateMessage> _privateMessagesCol;
        private readonly IMongoCollection<GroupMessage> _groupMessagesCol;
        public MessageRepository(IMongoDatabase mongoDb)
        {
            _privateMessagesCol = mongoDb.GetCollection<PrivateMessage>("privateMessages");
            _groupMessagesCol = mongoDb.GetCollection<GroupMessage>("groupMessages");
        }

        public async Task<List<PrivateMessage>> GetPrivateMessagesByIds(List<ObjectId> messageIds)
        {
            // Define a filter to match all the message IDs in the list
            var filter = Builders<PrivateMessage>.Filter.In(m => m.Id, messageIds);
            var privateMessages = await _privateMessagesCol.Find(filter).ToListAsync();
            return privateMessages;
        }

        // Fetch group messages by their IDs
        public async Task<List<GroupMessage>> GetGroupMessagesByIds(List<ObjectId> messageIds)
        {
            var filter = Builders<GroupMessage>.Filter.In(m => m.Id, messageIds);
            var groupMessages = await _groupMessagesCol.Find(filter).ToListAsync();
            return groupMessages;
        }

    }
}

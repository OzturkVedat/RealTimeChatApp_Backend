using MongoDB.Driver;
using RealTimeChatApp.API.Models;

namespace RealTimeChatApp.API.Services
{
    public class MessageService
    {
        private readonly IMongoCollection<MessageModel> _messagesCollection;

        public MessageService(IMongoDatabase mongoDb)
        {
            _messagesCollection = mongoDb.GetCollection<MessageModel>("messages");
        }

        public async Task SaveMessageAsync(MessageModel message)
        {
            await _messagesCollection.InsertOneAsync(message);
        }

        public async Task<List<MessageModel>> GetMessageHistory(string senderId, string recipientUserId, int limit = 50)
        {
            return await _messagesCollection
                .Find(m => m.SenderId == senderId || m.RecipientIds.Contains(recipientUserId))
                .SortByDescending(m => m.SentAt)
                .Limit(limit)
                .ToListAsync();
        }

        

    }
}

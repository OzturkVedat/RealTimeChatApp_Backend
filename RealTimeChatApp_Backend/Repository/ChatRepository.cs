using MongoDB.Bson;
using MongoDB.Driver;
using RealTimeChatApp.API.Interface;
using RealTimeChatApp.API.Models;

namespace RealTimeChatApp.API.Repository
{
    public class ChatRepository : IChatRepository
    {
        private readonly IMongoCollection<ChatModel> _chatsCollection;
        public ChatRepository(IMongoDatabase mongoDb)
        {
            _chatsCollection = mongoDb.GetCollection<ChatModel>("chats");
        }

        public async Task<ChatModel> GetChatById(ObjectId id) {
            return await _chatsCollection.Find(c=>c.Id == id).FirstOrDefaultAsync();
        }
        public async Task<List<ChatModel>> GetChatsByIds(List<ObjectId> ids)
        {
            var filter = Builders<ChatModel>.Filter.In(c => c.Id, ids);
            return await _chatsCollection.Find(filter).ToListAsync();
        }

        public async Task SaveChat(ChatModel chat)
        {
            await _chatsCollection.InsertOneAsync(chat);
        }
    }
}

using MongoDB.Bson;
using MongoDB.Driver;
using RealTimeChatApp.API.Interface;
using RealTimeChatApp.API.Models;
using RealTimeChatApp.API.DTOs.ResultModels;

namespace RealTimeChatApp.API.Repository
{
    public class ChatRepository : IChatRepository
    {
        private readonly IMongoCollection<ChatModel> _chatsCollection;
        private readonly ILogger<ChatRepository> _logger;   

        public ChatRepository(IMongoDatabase mongoDb, ILogger<ChatRepository> logger)
        {
            _chatsCollection = mongoDb.GetCollection<ChatModel>("chats");
            _logger = logger; 
        }

        public async Task<ResultModel> GetChatById(ObjectId id)
        {
            try
            {
                var chat = await _chatsCollection.Find(c => c.Id == id).FirstOrDefaultAsync();
                if (chat == null)
                {
                    _logger.LogWarning("Chat with ID {ChatId} not found.", id);
                    return new ErrorResult("Chat not found.", ErrorType.NotFound);
                }
                _logger.LogInformation("Chat with ID {ChatId} retrieved successfully.", id);
                return new SuccessDataResult<ChatModel>("Chat retrieved successfully.", chat);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the chat with ID {ChatId}.", id);
                return new ErrorResult("An error occurred while retrieving the chat.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> GetChatsByIds(List<ObjectId> ids)
        {
            try
            {
                var filter = Builders<ChatModel>.Filter.In(c => c.Id, ids);
                var chats = await _chatsCollection.Find(filter).ToListAsync();
                _logger.LogInformation("{Count} chats retrieved successfully.", chats.Count);
                return new SuccessDataResult<List<ChatModel>>("Chats retrieved successfully.", chats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving chats by IDs.");
                return new ErrorResult("An error occurred while retrieving chats.");
            }
        }

        public async Task<ResultModel> SaveChat(ChatModel chat)
        {
            try
            {
                await _chatsCollection.InsertOneAsync(chat);
                return new SuccessResult("Chat saved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while saving the chat.");
                return new ErrorResult("An error occurred while saving the chat.");
            }
        }
    }
}

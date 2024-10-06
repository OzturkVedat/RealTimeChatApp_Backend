using MongoDB.Bson;
using MongoDB.Driver;
using RealTimeChatApp.API.Interface;
using RealTimeChatApp.API.Models;
using Microsoft.Extensions.Logging;
using RealTimeChatApp.API.DTOs.ResultModels;

namespace RealTimeChatApp.API.Repository
{
    public class MessageRepository : IMessageRepository
    {
        private readonly IMongoCollection<PrivateMessage> _privateMessagesCol;
        private readonly IMongoCollection<GroupMessage> _groupMessagesCol;
        private readonly ILogger<MessageRepository> _logger;

        public MessageRepository(IMongoDatabase mongoDb, ILogger<MessageRepository> logger)
        {
            _privateMessagesCol = mongoDb.GetCollection<PrivateMessage>("privateMessages");
            _groupMessagesCol = mongoDb.GetCollection<GroupMessage>("groupMessages");
            _logger = logger;
        }

        public async Task<ResultModel> GetPrivateMessagesByIds(List<ObjectId> messageIds)
        {
            try
            {
                var filter = Builders<PrivateMessage>.Filter.In(m => m.Id, messageIds);
                var privateMessages = await _privateMessagesCol.Find(filter).ToListAsync();
                _logger.LogInformation("{Count} private messages retrieved successfully.", privateMessages.Count);
                return new SuccessDataResult<List<PrivateMessage>>("Private messages retrieved successfully.", privateMessages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving private messages by IDs.");
                return new ErrorResult("An error occurred while retrieving private messages.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> GetGroupMessagesByIds(List<ObjectId> messageIds)
        {
            try
            {
                var filter = Builders<GroupMessage>.Filter.In(m => m.Id, messageIds);
                var groupMessages = await _groupMessagesCol.Find(filter).ToListAsync();
                _logger.LogInformation("{Count} group messages retrieved successfully.", groupMessages.Count);
                return new SuccessDataResult<List<GroupMessage>>("Group messages retrieved successfully.", groupMessages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving group messages by IDs.");
                return new ErrorResult("An error occurred while retrieving group messages.", ErrorType.ServerError);
            }
        }
    }
}

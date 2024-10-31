using MongoDB.Bson;
using MongoDB.Driver;
using RealTimeChatApp.API.Interface;
using RealTimeChatApp.API.Models;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.ViewModels.ResultModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RealTimeChatApp.API.Repository
{
    public class MessageRepository : IMessageRepository
    {
        private readonly IMongoCollection<MessageModel> _messageCollection;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<MessageRepository> _logger;

        public MessageRepository(IMongoDatabase mongoDb, IUserRepository userRepository, ILogger<MessageRepository> logger)
        {
            _messageCollection = mongoDb.GetCollection<MessageModel>("messages");
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<ResultModel> GetMessageById(ObjectId messageId)
        {
            try
            {
                var message = await _messageCollection.Find(m => m.Id == messageId).FirstOrDefaultAsync();
                return message != null
                    ? new SuccessDataResult<MessageModel>("Message retrieved successfully.", message)
                    : new ErrorResult("Message not found.", ErrorType.NotFound);
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the message with ID {MessageId}.", messageId);
                return new ErrorResult("An error occurred while retrieving the message.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> GetMessagesByIds(List<ObjectId> messageIds)
        {
            try
            {
                var filter = Builders<MessageModel>.Filter.In(m => m.Id, messageIds);
                var messages = await _messageCollection.Find(filter).ToListAsync();
                return new SuccessDataResult<List<MessageModel>>("Messages retrieved successfully.", messages);
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving messages with IDs: {MessageIds}.", string.Join(", ", messageIds));
                return new ErrorResult("An error occurred while retrieving the messages.", ErrorType.ServerError);
            }
        }

        private async Task<string> GetMessageSenderFullname(string senderId)
        {
            try
            {
                var userResult = await _userRepository.GetUserById(senderId);
                return userResult is SuccessDataResult<UserModel> success ? success.Data.FullName : "No user found";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the sender's fullname for sender ID {SenderId}.", senderId);
                return "An error occurred while retrieving the sender's fullname.";
            }
        }

        public async Task<ResultModel> SaveNewMessage(MessageModel message)
        {
            try
            {
                await _messageCollection.InsertOneAsync(message);
                return new SuccessResult("Message saved successfully.");
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while saving a new message.");
                return new ErrorResult("An error occurred while saving the message.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> UpdateMessage(MessageModel message)
        {
            try
            {
                var filter = Builders<MessageModel>.Filter.Eq(m => m.Id, message.Id);
                var updateResult = await _messageCollection.ReplaceOneAsync(filter, message);
                return updateResult.IsAcknowledged && updateResult.ModifiedCount > 0
                    ? new SuccessResult("Message updated successfully.")
                    : new ErrorResult("Failed to update the message.");
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while updating the message with ID {MessageId}.", message.Id);
                return new ErrorResult("An error occurred while updating the message.", ErrorType.ServerError);
            }
        }
    }
}

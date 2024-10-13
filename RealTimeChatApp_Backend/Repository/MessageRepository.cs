using MongoDB.Bson;
using MongoDB.Driver;
using RealTimeChatApp.API.Interface;
using RealTimeChatApp.API.Models;
using Microsoft.Extensions.Logging;
using RealTimeChatApp.API.DTOs.ResultModels;
using System;
using RealTimeChatApp.API.ViewModels.ResultModels;

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
                var message = await _messageCollection.Find(c => c.Id == messageId).FirstOrDefaultAsync();
                if (message == null)
                    return new ErrorResult("Message not found.", ErrorType.NotFound);

                return new SuccessDataResult<MessageModel>("Message retrieved successfully.", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the message with ID {MessageId}.", messageId);
                return new ErrorResult("An error occurred while retrieving the chat.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> GetMessagesByIds(List<ObjectId> messageIds)
        {
            try
            {
                var filter = Builders<MessageModel>.Filter.In(m => m.Id, messageIds);
                var messages = await _messageCollection.Find(filter).ToListAsync();
                _logger.LogInformation("{Count} messages retrieved successfully.", messages.Count);
                return new SuccessDataResult<List<MessageModel>>("Messages retrieved successfully.", messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving messages by IDs.");
                return new ErrorResult("An error occurred while retrieving messages.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> GetMessageDetailsAsync(List<ObjectId> messageId)
        {
            try
            {
                var messageResult = await GetMessagesByIds(messageId);
                if (messageResult is SuccessDataResult<List<MessageModel>> success)
                {
                    var messages = success.Data;
                    var detailsList = new List<MessageDetailsRespone>();
                    foreach (var message in messages)
                    {
                        var details = new MessageDetailsRespone
                        {
                            MessageId = message.Id.ToString(),
                            Content = message.Content,
                            ReadStatus = message.ReadStatus,
                            SenderFullname = await GetMessageSenderFullname(message.SenderId),
                            SentAt = message.SentAt,
                        };
                        detailsList.Add(details);
                    }
                    
                    return new SuccessDataResult<List<MessageDetailsRespone>>("Message details retrieved", detailsList);
                }
                return messageResult;       // return error 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving details of messages.");
                return new ErrorResult("An error occurred while retrieving details of messages.", ErrorType.ServerError);
            }

        }
        private async Task<string> GetMessageSenderFullname(string senderId)
        {            
            var userResult = await _userRepository.GetUserById(senderId);
            if (userResult is SuccessDataResult<UserModel> success)
                return success.Data.FullName;
            return "No user found";
        }
        public async Task<ResultModel> SaveNewMessage(MessageModel message)
        {
            try
            {
                await _messageCollection.InsertOneAsync(message);
                return new SuccessResult("Message saved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while saving the message.");
                return new ErrorResult("An error occurred while saving the message.");
            }
        }

        public async Task<ResultModel> UpdateReadStatusOfMessage(ObjectId messageId, bool isRead)
        {
            try
            {
                var messageResult= await GetMessagesByIds(new List<ObjectId> { messageId });
                if(messageResult is SuccessDataResult<List<MessageModel>> successResult)
                {
                    var message= successResult.Data.FirstOrDefault();
                    if (message == null)
                    {
                        return new ErrorResult("Message not found.");
                    }
                    message.ReadStatus = isRead;
                    return await UpdateMessage(message);
                }
                return new ErrorResult("Failed to fetch the message.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the read status.");
                return new ErrorResult("An error occurred while updating the read status.");
            }

        }
        public async Task<ResultModel> UpdateMessage(MessageModel message)
        {
            try
            {
                var filter = Builders<MessageModel>.Filter.Eq(m => m.Id, message.Id);
                var updateResult = await _messageCollection.ReplaceOneAsync(filter, message);
                if (updateResult.IsAcknowledged && updateResult.ModifiedCount > 0)
                {
                    return new SuccessResult("Message updated successfully.");
                }
                return new ErrorResult("Failed to update the message.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the message.");
                return new ErrorResult("An error occurred while updating the message.");
            }
        }


    }
}

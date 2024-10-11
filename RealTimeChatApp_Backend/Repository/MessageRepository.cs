﻿using MongoDB.Bson;
using MongoDB.Driver;
using RealTimeChatApp.API.Interface;
using RealTimeChatApp.API.Models;
using Microsoft.Extensions.Logging;
using RealTimeChatApp.API.DTOs.ResultModels;
using System;

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
        public async Task<ResultModel> SaveNewMessage(MessageModel message)
        {
            try
            {
                if(message is PrivateMessage privateMessage)
                {
                    await _privateMessagesCol.InsertOneAsync(privateMessage);
                    return new SuccessResult("Message saved successfully.");
                }
                else if(message is GroupMessage groupMessage)
                {
                    await _groupMessagesCol.InsertOneAsync(groupMessage);
                    return new SuccessResult("Message saved successfully.");
                }
                return new ErrorResult("Failed to save the message");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while saving the message.");
                return new ErrorResult("An error occurred while saving the message.");
            }
        }

        public async Task<ResultModel> UpdateReadStatusOfMessage(MessageModel message)
        {
            if(message is PrivateMessage privateMessage)
            {
                var filter = Builders<PrivateMessage>.Filter.Eq(m => m.Id, privateMessage.Id);
                var update = Builders<PrivateMessage>.Update.Set(m => m.ReadStatus, privateMessage.ReadStatus);

                var result = await _privateMessagesCol.UpdateOneAsync(filter, update);
                if (result.ModifiedCount > 0)
                {
                    return new SuccessResult("Message read status updated.");
                }
                return new ErrorResult("Failed to update message.");
            }
            if (message is GroupMessage groupMessage) 
            {
                var filter = Builders<GroupMessage>.Filter.Eq(m => m.Id, groupMessage.Id);
                var update = Builders<GroupMessage>.Update.Set(m => m.ReadStatus, groupMessage.ReadStatus);

                var result = await _groupMessagesCol.UpdateOneAsync(filter, update);
                if (result.ModifiedCount > 0)
                {
                    return new SuccessResult("Group message read status updated.");
                }
                return new ErrorResult("Failed to update message.");
            }
            return new ErrorResult("Failed to update message.");
        }


    }
}

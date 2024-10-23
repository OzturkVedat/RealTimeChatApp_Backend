using MongoDB.Bson;
using MongoDB.Driver;
using RealTimeChatApp.API.Interface;
using RealTimeChatApp.API.Models;
using RealTimeChatApp.API.DTOs.ResultModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RealTimeChatApp.API.ViewModels.ResultModels;

namespace RealTimeChatApp.API.Repository
{
    public class ChatRepository : IChatRepository
    {
        private readonly IMongoCollection<ChatModel> _chatsCollection;
        private readonly IMessageRepository _messageRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<ChatRepository> _logger;

        public ChatRepository(IMongoDatabase mongoDb, IMessageRepository messageRepository,
            IUserRepository userRepository, ILogger<ChatRepository> logger)
        {
            _chatsCollection = mongoDb.GetCollection<ChatModel>("chats");
            _messageRepository = messageRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<ResultModel> GetPrivateChatById(ObjectId id)
        {
            try
            {
                var chat = await _chatsCollection.Find(c => c.Id == id).FirstOrDefaultAsync();
                return chat == null
                    ? new ErrorResult("Chat not found.", ErrorType.NotFound)
                    : new SuccessDataResult<ChatModel>("Chat retrieved successfully.", chat);
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the private chat.");
                return new ErrorResult("An error occurred while retrieving the requested chat.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> GetPrivateChatsByIds(List<ObjectId> ids)
        {
            try
            {
                var filter = Builders<ChatModel>.Filter.In(c => c.Id, ids);
                var chats = await _chatsCollection.Find(filter).ToListAsync();
                return new SuccessDataResult<List<ChatModel>>("Chats retrieved successfully.", chats);
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving chats by IDs: {ChatIds}.", string.Join(", ", ids));
                return new ErrorResult("An error occurred while retrieving chats by IDs.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> GetPrivateChatDetails(ObjectId chatId, string currentUserId)
        {
            try
            {
                var chatResult = await GetPrivateChatById(chatId);
                if (!chatResult.IsSuccess)
                    return chatResult;

                if (chatResult is SuccessDataResult<ChatModel> success)
                {
                    var recipientFullname = await GetChatRecipientFullname(success.Data, currentUserId);
                    var chatDetails = new ChatDetailsResponse
                    {
                        ChatId = success.Data.Id.ToString(),
                        LastMessage = success.Data.LastMessageContent,
                        LastMessageSender=success.Data.LastMessageSenderFullname,
                        RecipientFullname = recipientFullname
                    };
                    return new SuccessDataResult<ChatDetailsResponse>("Successfully fetched the chat details.", chatDetails);
                }

                return new ErrorResult("Error while fetching chat details.");
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving chat details.");
                return new ErrorResult("An error occurred while retrieving chat details.", ErrorType.ServerError);
            }

        }

        private async Task<string> GetChatRecipientFullname(ChatModel chat, string currentUserId)
        {
            var recipientId = chat.ParticipantIds.FirstOrDefault(id => id != currentUserId);
            if (string.IsNullOrEmpty(recipientId))
                return "No user found";

            var userResult = await _userRepository.GetUserById(recipientId);
            return userResult is SuccessDataResult<UserModel> success ? success.Data.FullName : "No user found";
        }

        public async Task<ResultModel> SavePrivateChat(ChatModel chat)
        {
            try
            {
                await _chatsCollection.InsertOneAsync(chat);
                return new SuccessResult("Chat saved successfully.");
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while saving the chat.");
                return new ErrorResult("An error occurred while saving chat.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> AddMessageToPrivateChat(ObjectId chatId, MessageModel message)
        {
            try
            {
                var filter = Builders<ChatModel>.Filter.Eq(c => c.Id, chatId);
                var update = Builders<ChatModel>.Update.AddToSet(c => c.MessageIds, message.Id)
                                                      .Set(c => c.LastMessageContent, message.Content)
                                                      .Set(c => c.LastMessageSenderFullname, message.SenderFullname);

                var updateResult = await _chatsCollection.UpdateOneAsync(filter, update);

                return updateResult.ModifiedCount > 0
                    ? new SuccessResult("Message added to chat successfully.")
                    : new ErrorResult("Chat not found.", ErrorType.NotFound);
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while adding the message to the chat with ID {ChatId}.", chatId);
                return new ErrorResult("An error occurred while adding the message to the chat.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> UpdatePrivateChat(ChatModel chat)
        {
            try
            {
                var filter = Builders<ChatModel>.Filter.Eq(c => c.Id, chat.Id);
                var updateResult = await _chatsCollection.ReplaceOneAsync(filter, chat);

                return updateResult.MatchedCount > 0
                    ? new SuccessResult("Chat updated successfully.")
                    : new ErrorResult("Chat not found.", ErrorType.NotFound);
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while updating chat with ID {ChatId}.", chat.Id);
                return new ErrorResult($"An error occurred while updating the requested chat.", ErrorType.ServerError);
            }
        }
    }
}

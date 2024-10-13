using MongoDB.Bson;
using MongoDB.Driver;
using RealTimeChatApp.API.Interface;
using RealTimeChatApp.API.Models;
using RealTimeChatApp.API.DTOs.ResultModels;
using System;
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
        public async Task<ResultModel> GetChatDetailsAsync(ObjectId chatId, string currentUserId)
        {
            var chatResult = await GetChatById(chatId);
            if (!chatResult.IsSuccess)
                return chatResult;

            if (chatResult is SuccessDataResult<ChatModel> success)
            {
                var chat = success.Data;

                var recipientFullname = await GetChatRecipientFullname(chat, currentUserId);

                var chatDetails = new ChatDetailsResponse
                {
                    ChatId = chat.Id.ToString(),
                    LastMessage = chat.LastMessageContent,
                    RecipientFullname = recipientFullname
                };

                return new SuccessDataResult<ChatDetailsResponse>("Successfully fetched the chat details.", chatDetails);
            }
            return new ErrorResult("Error while fetching chat details.");
        }

        private async Task<string> GetChatRecipientFullname(ChatModel chat, string currentUserId)
        {
            var idList = chat.ParicipantIds.Where(id => id != currentUserId).ToList();
            if (!idList.Any())
                return "No user found";

            var userResult = await _userRepository.GetUserById(idList.First());
            if (userResult is SuccessDataResult<UserModel> success)
                return success.Data.FullName;

            return "No user found";
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
        public async Task<ResultModel> AddMessageToChat(ObjectId chatId, MessageModel message)
        {
            try
            {
                var filter = Builders<ChatModel>.Filter.Eq(c => c.Id, chatId);
                var update = Builders<ChatModel>.Update.AddToSet(c => c.MessageIds, message.Id)
                                                      .Set(c => c.LastMessageContent, message.Content);
                var updateResult = await _chatsCollection.UpdateOneAsync(filter, update);

                if (updateResult.ModifiedCount > 0)
                {
                    _logger.LogInformation("Message with ID {MessageId} added to chat {ChatId}.", message.Id, chatId);
                    return new SuccessResult("Message added to chat successfully.");
                }
                _logger.LogWarning("Chat with ID {ChatId} not found.", chatId);
                return new ErrorResult("Chat not found.", ErrorType.NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding message {MessageId} to chat {ChatId}.", message.Id, chatId);
                return new ErrorResult("An error occurred while adding the message to the chat.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> UpdateChat(ChatModel chat)
        {
            try
            {
                var filter = Builders<ChatModel>.Filter.Eq(c => c.Id, chat.Id);
                var updateResult = await _chatsCollection.ReplaceOneAsync(filter, chat);

                if (updateResult.MatchedCount > 0)
                {
                    _logger.LogInformation("Chat with ID {ChatId} updated successfully.", chat.Id);
                    return new SuccessResult("Chat updated successfully.");
                }

                _logger.LogWarning("Chat with ID {ChatId} not found.", chat.Id);
                return new ErrorResult("Chat not found.", ErrorType.NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating chat {ChatId}.", chat.Id);
                return new ErrorResult("An error occurred while updating the chat.", ErrorType.ServerError);

            }
        }
    }
}

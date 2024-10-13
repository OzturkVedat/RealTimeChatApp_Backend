using MongoDB.Bson;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Models;

namespace RealTimeChatApp.API.Interface
{
    public interface IChatRepository
    {
        Task<ResultModel> GetChatById(ObjectId chatId);
        Task<ResultModel> GetChatsByIds(List<ObjectId> ids);
        Task<ResultModel> GetChatDetailsAsync(ObjectId chatId, string currentUserId);
        Task<ResultModel> SaveChat(ChatModel chat);
        Task<ResultModel> AddMessageToChat(ObjectId chatId, MessageModel messageId);
        Task<ResultModel> UpdateChat(ChatModel chat);
    }
}

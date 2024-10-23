using MongoDB.Bson;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Models;

namespace RealTimeChatApp.API.Interface
{
    public interface IChatRepository
    {
        Task<ResultModel> GetPrivateChatById(ObjectId chatId);
        Task<ResultModel> GetPrivateChatsByIds(List<ObjectId> ids);
        Task<ResultModel> GetPrivateChatDetails(ObjectId chatId, string currentUserId);
        Task<ResultModel> SavePrivateChat(ChatModel chat);
        Task<ResultModel> AddMessageToPrivateChat(ObjectId chatId, MessageModel messageId);
        Task<ResultModel> UpdatePrivateChat(ChatModel chat);
    }
}

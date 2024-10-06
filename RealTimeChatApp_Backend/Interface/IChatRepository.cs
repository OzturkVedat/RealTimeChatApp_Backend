using MongoDB.Bson;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Models;

namespace RealTimeChatApp.API.Interface
{
    public interface IChatRepository
    {
        Task<ResultModel> GetChatById(ObjectId chatId);
        Task<ResultModel> GetChatsByIds(List<ObjectId> ids);
        Task<ResultModel> SaveChat(ChatModel chat);
    }
}

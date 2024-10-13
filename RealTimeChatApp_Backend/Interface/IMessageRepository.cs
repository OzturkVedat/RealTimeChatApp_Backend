using MongoDB.Bson;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Models;

namespace RealTimeChatApp.API.Interface
{
    public interface IMessageRepository
    {
        Task<ResultModel> GetMessageById(ObjectId messageId);
        Task<ResultModel> GetMessagesByIds(List<ObjectId> messageIds);
        Task<ResultModel> GetMessageDetailsAsync(List<ObjectId> messageId);
        Task<ResultModel> SaveNewMessage(MessageModel message);
        Task<ResultModel> UpdateReadStatusOfMessage(ObjectId messageId, bool isRead);
        Task<ResultModel> UpdateMessage(MessageModel message);
    }
}

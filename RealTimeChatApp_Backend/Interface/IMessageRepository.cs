using MongoDB.Bson;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Models;

namespace RealTimeChatApp.API.Interface
{
    public interface IMessageRepository
    {
        //Task<MessageModel> GetPrivateMessageById(ObjectId id);
        Task<ResultModel> GetPrivateMessagesByIds(List<ObjectId> messageIds);
        Task<ResultModel> GetGroupMessagesByIds(List<ObjectId> messageIds);

    }
}

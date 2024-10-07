using MongoDB.Bson;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Models;

namespace RealTimeChatApp.API.Interface
{
    public interface IMessageRepository
    {
        Task<ResultModel> GetPrivateMessagesByIds(List<ObjectId> messageIds);
        Task<ResultModel> GetGroupMessagesByIds(List<ObjectId> messageIds);
        Task<ResultModel> UpdatePrivateMessage(PrivateMessage message);
        Task<ResultModel> UpdateGroupMessage(GroupMessage message);

    }
}

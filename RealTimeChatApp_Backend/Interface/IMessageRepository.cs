using MongoDB.Bson;
using RealTimeChatApp.API.Models;

namespace RealTimeChatApp.API.Interface
{
    public interface IMessageRepository
    {
        //Task<MessageModel> GetPrivateMessageById(ObjectId id);
        Task<List<PrivateMessage>> GetPrivateMessagesByIds(List<ObjectId> messageIds);
        Task<List<GroupMessage>> GetGroupMessagesByIds(List<ObjectId> messageIds);

    }
}

using MongoDB.Bson;
using RealTimeChatApp.API.Models;

namespace RealTimeChatApp.API.Interface
{
    public interface IChatRepository
    {
        Task<ChatModel> GetChatById(ObjectId chatId);
        Task<List<ChatModel>> GetChatsByIds(List<ObjectId> ids);
        Task SaveChat(ChatModel chat);  
    }
}

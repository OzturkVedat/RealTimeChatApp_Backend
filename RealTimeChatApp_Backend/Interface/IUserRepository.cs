using Microsoft.AspNetCore.Identity.Data;
using MongoDB.Bson;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Models;

namespace RealTimeChatApp.API.Interface
{
    public interface IUserRepository
    {
        Task <UserModel> GetUserById(string id);
        Task<List<ObjectId>> GetLastUserChatsById(string userId, int limit);
        Task<List<string>> GetUserFriendIds(string userId);
        Task<Dictionary<string, string>> GetUserFriendFullnames(List<string> friendIds);
        Task<ResultModel> SaveUserFriendByEmail(string userId, string email);
        Task SaveUserChatById(string userId, ObjectId chatId);
        Task UpdateUser(UserModel model);
    }
}

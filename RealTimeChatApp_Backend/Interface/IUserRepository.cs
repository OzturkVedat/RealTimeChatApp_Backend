using Microsoft.AspNetCore.Identity.Data;
using MongoDB.Bson;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Models;

namespace RealTimeChatApp.API.Interface
{
    public interface IUserRepository
    {
        Task <ResultModel> GetUserById(string id);
        Task<ResultModel> GetLastUserChatsById(string userId, int limit);
        Task<ResultModel> GetUserFriendIds(string userId);
        Task<ResultModel> GetUserFriendFullnames(List<string> friendIds);
        Task<ResultModel> AddUserFriendByEmail(string userId, string email);
        Task<ResultModel> AddUserChatById(string userId, ObjectId chatId);
        Task<ResultModel> UpdateUser(UserModel model);
    }
}

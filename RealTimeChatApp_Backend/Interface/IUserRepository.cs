using Microsoft.AspNetCore.Identity.Data;
using MongoDB.Bson;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Models;
using System.Linq.Expressions;

namespace RealTimeChatApp.API.Interface
{
    public interface IUserRepository
    {
        Task <ResultModel> GetUserById(string id);
        Task<ResultModel> GetUserChatIds(string userId);
        Task<ResultModel> GetUserChatCount(string userId);
        Task<ResultModel> GetUserFriendIds(string userId);
        Task<ResultModel> GetUserFriendsFullnames(List<string> friendIds);
        Task<ResultModel> GetUserFriendsOnlineStatus(List<string> friendIds);
        Task<ResultModel> AddUserFriendByEmail(string userId, string email);
        Task<ResultModel> AddUserChatById(string userId, ObjectId chatId);
        Task<ResultModel> UpdateUser(UserModel model);
        Task<ResultModel> UpdateUserStatus(string userId, bool isOnline);
    }
}

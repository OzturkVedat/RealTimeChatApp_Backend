using Microsoft.AspNetCore.Identity.Data;
using MongoDB.Bson;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Models;
using System.Linq.Expressions;

namespace RealTimeChatApp.API.Interface
{
    public interface IUserRepository
    {
        Task<ResultModel> GetUserById(string id);
        Task<ResultModel> GetUserIdsByType(string userId, string idType);
        Task<ResultModel> GetUserFriendsFullnames(List<string> friendIds);
        Task<ResultModel> GetUserFriendsOnlineStatus(List<string> friendIds);
        Task<ResultModel> AddUserFriendByEmail(string userId, string email);
        Task<ResultModel> AddUserPrivateChatById(string userId, ObjectId chatId);
        Task<ResultModel> AddUserGroupById(string userId, ObjectId groupId);
        Task<ResultModel> UpdateUser(UserModel model);
        Task<ResultModel> UpdateUserStatus(string userId, bool isOnline);
        Task<ResultModel> RemoveUserGroupById(string userId, ObjectId groupId);
    }
}

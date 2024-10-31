using Microsoft.AspNetCore.Identity.Data;
using MongoDB.Bson;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Models;
using RealTimeChatApp.API.ViewModels.RequestModels;
using System.Linq.Expressions;

namespace RealTimeChatApp.API.Interface
{
    public interface IUserRepository
    {
        Task<ResultModel> GetUserById(string id);
        Task<ResultModel> GetAuthenticatedUserDetails(string userId);
        Task<ResultModel> GetSuperadminById(string userId);
        Task<ResultModel> GetUserIdsByType(string userId, string idType);
        Task<ResultModel> GetUserFriendsDetails(List<string> friendIds);
        Task<ResultModel> SearchFriendsByFullname(string fullname);
        Task<ResultModel> AddUserFriendById(string userId, string friendId);
        Task<ResultModel> AddUserPrivateChatById(string userId, ObjectId chatId);
        Task<ResultModel> AddUserGroupById(string userId, ObjectId groupId);
        Task<ResultModel> UpdateUserStatus(string userId, bool isOnline);
        Task<ResultModel> RemoveUserGroupById(string userId, ObjectId groupId);
    }
}

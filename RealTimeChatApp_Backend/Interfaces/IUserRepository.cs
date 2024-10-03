using Microsoft.AspNetCore.Identity.Data;
using RealTimeChatApp.API.Models;

namespace RealTimeChatApp.API.Interfaces
{
    public interface IUserRepository
    {
        Task<UserModel> GetUserById(string userId);
        Task CreateUser(UserModel user);
        Task UpdateUser(RegisterRequest updatedUser);
    }
}

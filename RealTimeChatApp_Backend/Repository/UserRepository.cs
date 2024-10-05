using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using MongoDB.Bson;
using MongoDB.Driver;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Interface;
using RealTimeChatApp.API.Models;
using SharpCompress.Common;

namespace RealTimeChatApp.API.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly IMongoCollection<UserModel> _usersCollection;
        public UserRepository(IMongoDatabase mongoDb)
        {
            _usersCollection = mongoDb.GetCollection<UserModel>("users");
        }
        public async Task<UserModel> GetUserById(string id)
        {
            return await _usersCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
           
        }

        public async Task<List<ObjectId>> GetLastUserChatsById(string userId, int limit)
        {
            var user = await GetUserById(userId);
            return user?.ChatIds?.Take(limit).ToList() ?? new List<ObjectId>();
        }

        public async Task<List<string>> GetUserFriendIds(string userId)
        {
            var user = await GetUserById(userId);
            return user.FriendsListIds;
        }

        public async Task<Dictionary<string,string>> GetUserFriendFullnames(List<string> friendIds)
        {
            var filter = Builders<UserModel>.Filter.In(u => u.Id, friendIds); 
            var projection = Builders<UserModel>.Projection.Include(u => u.Email).Include(u => u.FullName);

            var users = await _usersCollection.Find(filter)
                                              .Project<UserModel>(projection)
                                              .ToListAsync();

            var userDetails = new Dictionary<string, string>();
            foreach (var user in users)
                userDetails[user.Id] = user.FullName;
            
            return userDetails;
        }

        public async Task<ResultModel> SaveUserFriendByEmail(string userId, string email)
        {
            var friendUser = await _usersCollection.Find(u => u.Email == email).FirstOrDefaultAsync();

            if (friendUser == null)
                return new ErrorResult("User not found for the given email.");

            var currentUser = await GetUserById(userId);
            if (!currentUser.FriendsListIds.Contains(friendUser.Id))
            {
                currentUser.FriendsListIds.Add(friendUser.Id);
                await UpdateUser(currentUser);
                return new SuccessResult("Friend added.");
            }
            return new ErrorResult("User already has this friend on the friends list.");
        }

        public async Task SaveUserChatById(string userId,ObjectId chatId)
        {
            var user = await _usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();
            user.ChatIds.Add(chatId);
            await UpdateUser(user);
        }


        public async Task UpdateUser(UserModel updatedModel)
        {
            await _usersCollection.ReplaceOneAsync(m => m.Id == updatedModel.Id, updatedModel);
        }    

    }
}

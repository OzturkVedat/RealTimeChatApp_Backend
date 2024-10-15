using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using MongoDB.Bson;
using MongoDB.Driver;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Interface;
using RealTimeChatApp.API.Models;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace RealTimeChatApp.API.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly IMongoCollection<UserModel> _usersCollection;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(IMongoDatabase mongoDb, ILogger<UserRepository> logger)
        {
            _usersCollection = mongoDb.GetCollection<UserModel>("users");
            _logger = logger;
        }

        public async Task<ResultModel> GetUserById(string id)
        {
            try
            {
                var user = await _usersCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found.", id);
                    return new ErrorResult("User not found.", ErrorType.NotFound);
                }

                _logger.LogInformation("User with ID {UserId} retrieved successfully.", id);
                return new SuccessDataResult<UserModel>("User retrieved successfully.", user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving user with ID {UserId}.", id);
                return new ErrorResult("An error occurred while retrieving the user.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> GetUserChatCount(string userId)
        {
            try
            {
                var userResult = await GetUserById(userId);
                if (!userResult.IsSuccess)
                    return userResult;
                if (userResult is SuccessDataResult<UserModel> successResult)
                {
                    int chatCount = successResult.Data.ChatIds.Count;
                    return new SuccessDataResult<int>("Successfully fetched the count", chatCount);
                }
                return new ErrorResult("Error while chat count");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching user chat counts for user ID {UserId}.", userId);
                return new ErrorResult("An error occurred while fetching user chat count.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> GetUserChatIds(string userId)
        {
            try
            {
                var userResult = await GetUserById(userId);
                if(userResult is SuccessDataResult<UserModel> success)
                {
                    var chatIds= success.Data.ChatIds.ToList();
                    return new SuccessDataResult<List<ObjectId>>("Successfully fetched the last user chat IDs", chatIds);
                }
                return userResult;      // errors
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching last user chats for user ID {UserId}.", userId);
                return new ErrorResult("An error occurred while fetching last user chats.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> GetUserFriendIds(string userId)
        {
            try
            {
                var userResult = await GetUserById(userId);
                if (userResult is SuccessDataResult<UserModel> successResult)
                {
                    var idList = successResult.Data.FriendsListIds ?? new List<string>();
                    return new SuccessDataResult<List<string>>("User friend IDs retrieved successfully.", idList);
                }
                return userResult;      // return error result
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching friend IDs for user ID {UserId}.", userId);
                return new ErrorResult("An error occurred while fetching user friend IDs.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> GetUserFriendsFullnames(List<string> friendIds)
        {
            try
            {
                var filter = Builders<UserModel>.Filter.In(u => u.Id, friendIds);
                var projection = Builders<UserModel>.Projection.Include(u => u.FullName); 
                var users = await _usersCollection.Find(filter)
                                                   .Project<UserModel>(projection)
                                                   .ToListAsync();
                var userDetails = new Dictionary<string, string>();
                foreach (var user in users)
                {
                    userDetails[user.Id] = user.FullName;
                }
                return new SuccessDataResult<Dictionary<string, string>>(
                    "User Fullnames retrieved successfully.",
                    userDetails
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching user friend fullnames.");
                return new ErrorResult("An error occurred while fetching user friend fullnames.", ErrorType.ServerError);
            }
        }
        public async Task<ResultModel> GetUserFriendsOnlineStatus(List<string> friendIds)
        {
            try
            {
                var filter = Builders<UserModel>.Filter.In(u => u.Id, friendIds);
                var projection = Builders<UserModel>.Projection.Include(u => u.isOnline);  // Project OnlineStatus
                var users = await _usersCollection.Find(filter)
                                                   .Project<UserModel>(projection)
                                                   .ToListAsync();

                var userDetails = new Dictionary<string, bool>();
                foreach (var user in users)
                {
                    userDetails[user.Id] = user.isOnline;
                    _logger.LogInformation($"Friend ID: {user.Id}, Online Status: {user.isOnline}");
                }
                return new SuccessDataResult<Dictionary<string, bool>>(
                    "User Online Status retrieved successfully.",
                    userDetails
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching user friend online statuses.");
                return new ErrorResult("An error occurred while fetching user friend online statuses.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> AddUserFriendByEmail(string userId, string email)
        {
            try
            {
                var friendUser = await _usersCollection.Find(u => u.Email == email).FirstOrDefaultAsync();
                if (friendUser == null)
                {
                    _logger.LogWarning("User not found for the given email: {Email}.", email);
                    return new ErrorResult("User not found for the given email.", ErrorType.NotFound);
                }
                var currentUserResult = await GetUserById(userId);
                if (!currentUserResult.IsSuccess)
                    return currentUserResult; // Return the error from GetUserById if the user is not found.

                var currentUser = ((SuccessDataResult<UserModel>)currentUserResult).Data;
                if (!currentUser.FriendsListIds.Contains(friendUser.Id))
                {
                    currentUser.FriendsListIds.Add(friendUser.Id);
                    friendUser.FriendsListIds.Add(currentUser.Id);

                    await Task.WhenAll(UpdateUser(currentUser), UpdateUser(friendUser));

                    _logger.LogInformation("Friend with ID {FriendId} added to user with ID {UserId}.", friendUser.Id, userId);
                    return new SuccessDataResult<string>("Friend with the ID added:", friendUser.Id);
                }

                _logger.LogWarning("User with ID {UserId} already has friend with ID {FriendId} on their friends list.", userId, friendUser.Id);
                return new ErrorResult("User already has this friend on the friends list.", ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding friend by email for user ID {UserId}.", userId);
                return new ErrorResult("An error occurred while adding friend.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> AddUserChatById(string userId, ObjectId chatId)
        {
            try
            {
                var userResult = await GetUserById(userId);
                if (!userResult.IsSuccess)
                    return userResult;

                var user = ((SuccessDataResult<UserModel>)userResult).Data;
                user.ChatIds.Add(chatId);
                await UpdateUser(user);
                _logger.LogInformation("Chat ID {ChatId} saved for user ID {UserId}.", chatId, userId);
                return new SuccessDataResult<ObjectId>("Chat with the ID saved successfully.", chatId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding chat for user ID {UserId}.", userId);
                return new ErrorResult("An error occurred while adding chat.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> UpdateUser(UserModel updatedModel)
        {
            try
            {
                await _usersCollection.ReplaceOneAsync(m => m.Id == updatedModel.Id, updatedModel);
                _logger.LogInformation("User with ID {UserId} updated successfully.", updatedModel.Id);
                return new SuccessResult("User updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating user ID {UserId}.", updatedModel.Id);
                return new ErrorResult("An error occurred while updating the user.", ErrorType.ServerError);
            }
        }
        public async Task<ResultModel> UpdateUserStatus(string userId, bool isOnline)
        {
            try
            {
                var updateDefinition = Builders<UserModel>.Update.Set(u => u.isOnline, isOnline);
                var result = await _usersCollection.UpdateOneAsync(u => u.Id == userId, updateDefinition);
                if (result.ModifiedCount > 0)
                {
                    _logger.LogInformation("User status updated successfully for user ID {UserId}.", userId);
                    return new SuccessResult("User status updated.");
                }
                return new ErrorResult("Failed to update: User not found.");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured while updating the user online status.");
                return new ErrorResult("Error occured while updating the user online status.");
            }
        }

    }
}

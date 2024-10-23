using MongoDB.Bson;
using MongoDB.Driver;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Interface;
using RealTimeChatApp.API.Models;

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
                return user == null
                    ? new ErrorResult("User not found.", ErrorType.NotFound)
                    : new SuccessDataResult<UserModel>("User retrieved successfully.", user);
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the user");
                return new ErrorResult("An error occurred while retrieving the user.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> GetUserIdsByType(string userId, string idType)
        {
            try
            {
                var userResult = await GetUserById(userId);
                if (userResult is not SuccessDataResult<UserModel> success)
                    return userResult; // return error result

                return idType switch
                {
                    "chatIds" => new SuccessDataResult<List<ObjectId>>("Successfully fetched the user chat IDs", success.Data.ChatIds),
                    "groupIds" => new SuccessDataResult<List<ObjectId>>("Successfully fetched the user group IDs", success.Data.GroupIds),
                    "friendIds" => new SuccessDataResult<List<string>>("Successfully fetched the user friend IDs", success.Data.FriendsListIds),
                    _ => new ErrorResult($"Invalid idType: {idType}")
                };
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the user IDs");
                return new ErrorResult("An error occurred while retrieving user IDs.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> GetUserFriendsFullnames(List<string> friendIds)
        {
            try
            {
                var filter = Builders<UserModel>.Filter.In(u => u.Id, friendIds);
                var projection = Builders<UserModel>.Projection.Include(u => u.FullName);
                var users = await _usersCollection.Find(filter).Project<UserModel>(projection).ToListAsync();

                var userDetails = users.ToDictionary(user => user.Id, user => user.FullName);
                return new SuccessDataResult<Dictionary<string, string>>("User Fullnames retrieved successfully.", userDetails);
            }
            catch (MongoException ex)
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
                var projection = Builders<UserModel>.Projection.Include(u => u.IsOnline);
                var users = await _usersCollection.Find(filter).Project<UserModel>(projection).ToListAsync();

                var userDetails = users.ToDictionary(user => user.Id, user => user.IsOnline);
                return new SuccessDataResult<Dictionary<string, bool>>("User Online Status retrieved successfully.", userDetails);
            }
            catch (MongoException ex)
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
                    return new ErrorResult("User not found for the given email.", ErrorType.NotFound);
                }

                var currentUserResult = await GetUserById(userId);
                if (!currentUserResult.IsSuccess) return currentUserResult; // return the error from GetUserById

                var currentUser = ((SuccessDataResult<UserModel>)currentUserResult).Data;
                if (!currentUser.FriendsListIds.Contains(friendUser.Id))
                {
                    await _usersCollection.UpdateOneAsync(u => u.Id == userId, Builders<UserModel>.Update.AddToSet(u => u.FriendsListIds, friendUser.Id));
                    if (userId != friendUser.Id) // in case the user adds himself as a friend
                        await _usersCollection.UpdateOneAsync(u => u.Id == friendUser.Id, Builders<UserModel>.Update.AddToSet(u => u.FriendsListIds, userId));

                    return new SuccessDataResult<string>("Friend with the ID added:", friendUser.Id);
                }
                return new ErrorResult("User already has this friend on the friends list.", ErrorType.Conflict);
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while adding a friend.");
                return new ErrorResult("An error occurred while adding a friend.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> AddUserPrivateChatById(string userId, ObjectId chatId)
        {
            try
            {
                var userResult = await GetUserById(userId);
                if (!userResult.IsSuccess) return userResult;

                var user = ((SuccessDataResult<UserModel>)userResult).Data;
                await _usersCollection.UpdateOneAsync(u => u.Id == userId, Builders<UserModel>.Update.AddToSet(u => u.ChatIds, chatId));
                return new SuccessDataResult<ObjectId>("Chat with the ID saved successfully.", chatId);
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while adding the private chat.");
                return new ErrorResult("An error occurred while adding the private chat.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> AddUserGroupById(string userId, ObjectId groupId)
        {
            try
            {
                var userResult = await GetUserById(userId);
                if (!userResult.IsSuccess) return userResult;

                var user = ((SuccessDataResult<UserModel>)userResult).Data;
                await _usersCollection.UpdateOneAsync(u => u.Id == userId, Builders<UserModel>.Update.AddToSet(u => u.GroupIds, groupId));
                return new SuccessDataResult<ObjectId>("Group with the ID saved successfully.", groupId);
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while adding the group.");
                return new ErrorResult("An error occurred while adding the group.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> UpdateUser(UserModel updatedModel)
        {
            try
            {
                var result = await _usersCollection.ReplaceOneAsync(m => m.Id == updatedModel.Id, updatedModel);
                return result.IsAcknowledged && result.ModifiedCount > 0
                    ? new SuccessResult("User updated successfully.")
                    : new ErrorResult("Failed to update: User not found.");
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while updating the user.");
                return new ErrorResult("An error occurred while updating the user.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> UpdateUserStatus(string userId, bool isOnline)
        {
            try
            {
                var updateDefinition = Builders<UserModel>.Update.Set(u => u.IsOnline, isOnline);
                var result = await _usersCollection.UpdateOneAsync(u => u.Id == userId, updateDefinition);
                return result.ModifiedCount > 0
                    ? new SuccessResult("User status updated.")
                    : new ErrorResult("Failed to update: User not found.");
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while updating user status.");
                return new ErrorResult("An error occurred while updating user status.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> RemoveUserGroupById(string userId, ObjectId groupId)
        {
            try
            {
                var userResult = await GetUserById(userId);
                if (!userResult.IsSuccess) return userResult;

                var user = ((SuccessDataResult<UserModel>)userResult).Data;
                await _usersCollection.UpdateOneAsync(u => u.Id == userId, Builders<UserModel>.Update.Pull(u => u.GroupIds, groupId));
                return new SuccessDataResult<ObjectId>("Group with the ID removed successfully.", groupId);
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while removing the group.");
                return new ErrorResult("An error occurred while removing the group.", ErrorType.ServerError);
            }
        }
    }
}

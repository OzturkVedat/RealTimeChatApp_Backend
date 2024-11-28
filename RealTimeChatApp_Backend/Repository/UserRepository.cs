using MongoDB.Bson;
using MongoDB.Driver;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Interface;
using RealTimeChatApp.API.Models;
using RealTimeChatApp.API.ViewModels.RequestModels;
using RealTimeChatApp.API.ViewModels.ResultModels;
using System.Xml.Linq;

namespace RealTimeChatApp.API.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly IMongoCollection<UserModel> _usersCollection;
        private readonly IMongoCollection<Superadmin> _superadminCollection;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(IMongoDatabase mongoDb, ILogger<UserRepository> logger)
        {
            _usersCollection = mongoDb.GetCollection<UserModel>("users");
            _superadminCollection = mongoDb.GetCollection<Superadmin>("superadmins");
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
        public async Task<ResultModel> GetAuthenticatedUserDetails(string userId)
        {
            try
            {
                var projection = Builders<UserModel>.Projection
                    .Include(u => u.Id)
                    .Include(u => u.FullName)
                    .Include(u => u.Email)
                    .Include(u => u.ProfilePictureUrl)
                    .Include(u => u.IsOnline);

                var user = await _usersCollection
                    .Find(x => x.Id == userId)
                    .Project<UserModel>(projection)
                    .FirstOrDefaultAsync();

                if (user == null)
                    return new ErrorResult("User not found.", ErrorType.NotFound);

                var details = new UserDetailsResponse
                {
                    Id = user.Id,
                    Fullname = user.FullName,
                    Email = user.Email,
                    ProfilePicUrl = user.ProfilePictureUrl,
                    IsOnline = user.IsOnline
                };
                return new SuccessDataResult<UserDetailsResponse>("Successfully fetched the user details.", details);
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the user");
                return new ErrorResult("An error occurred while retrieving the user.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> GetSuperadminById(string userId)
        {
            var super = await _superadminCollection.Find(s => s.UserId == userId).FirstOrDefaultAsync();
            return super != null ? new SuccessDataResult<Superadmin>("Successfull.", super) : new ErrorResult("Not found.");
        }

        public async Task<ResultModel> GetAllBroadcastedIds()
        {
            try
            {
                var superadmins = await _superadminCollection.Find(FilterDefinition<Superadmin>.Empty).ToListAsync();
                var broadcastedIds = superadmins.SelectMany(s => s.BroadcastedIds).Distinct().ToList();

                return new SuccessDataResult<List<ObjectId>>("Successfully fetched broadcasted IDs.", broadcastedIds);
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving broadcasted IDs.");
                return new ErrorResult("An error occurred while retrieving broadcasted IDs.", ErrorType.ServerError);
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

        public async Task<ResultModel> GetUserFriendsDetails(List<string> friendIds)
        {
            try
            {
                var filter = Builders<UserModel>.Filter.In(u => u.Id, friendIds);
                var projection = Builders<UserModel>.Projection
                    .Include(u => u.Id)
                    .Include(u => u.FullName)
                    .Include(u => u.ProfilePictureUrl)
                    .Include(u => u.StatusMessage)
                    .Include(u => u.IsOnline);

                var friendDetails = await _usersCollection
                    .Find(filter)
                    .Project(u => new FriendDetailsResponse     // project directly
                    {
                        Id = u.Id,
                        Fullname = u.FullName,
                        PictureUrl = u.ProfilePictureUrl,
                        StatusMessage = u.StatusMessage,
                        IsOnline = u.IsOnline
                    })
                    .ToListAsync();
                return new SuccessDataResult<List<FriendDetailsResponse>>("User friend details retrieved successfully.", friendDetails);
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while fetching user friend details.");
                return new ErrorResult("An error occurred while fetching user friend details.", ErrorType.ServerError);
            }
        }


        public async Task<ResultModel> SearchFriendsByFullname(string fullname)
        {
            try
            {
                var filter = Builders<UserModel>.Filter.Regex(u => u.FullName, new MongoDB.Bson.BsonRegularExpression(fullname, "i")); // 'i' for case-insensitive
                var results = await _usersCollection.Find(filter)
                                         .Limit(20)
                                         .ToListAsync();

                var searchResults = results.Select(user => new SearchDetailsResponse
                {
                    UserId = user.Id,
                    Fullname = user.FullName,
                    UserPictureUrl = user.ProfilePictureUrl
                }).ToList();

                return results != null ?
                    new SuccessDataResult<List<SearchDetailsResponse>>("Successfully fetched the search results.", searchResults) :
                    new ErrorResult("No user found.");
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while fetching user names.");
                return new ErrorResult("An error occurred while fetching user names.", ErrorType.ServerError);
            }
        }

        
        public async Task<ResultModel> AddUserFriendById(string userId, string newFriendId)
        {
            try
            {
                var filter = Builders<UserModel>.Filter.Eq(u => u.Id, newFriendId);
                var friendExists = await _usersCollection.Find(filter).Limit(1).AnyAsync();
                if (!friendExists)
                    return new ErrorResult("User not found for the given ID.", ErrorType.NotFound);

                var currentUserResult = await GetUserById(userId);
                if (!currentUserResult.IsSuccess) return currentUserResult; // return the error from GetUserById

                var currentUser = ((SuccessDataResult<UserModel>)currentUserResult).Data;
                if (!currentUser.FriendsListIds.Contains(newFriendId))
                {
                    await _usersCollection.UpdateOneAsync(u => u.Id == userId, Builders<UserModel>.Update
                                    .AddToSet(u => u.FriendsListIds, newFriendId));

                    if (userId != newFriendId) // in case the user adds himself as a friend
                        await _usersCollection.UpdateOneAsync(u => u.Id == newFriendId, Builders<UserModel>.Update.AddToSet(u => u.FriendsListIds, userId));

                    return new SuccessResult("Friend added.");
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

                if (user.GroupIds.Contains(groupId))
                    return new ErrorResult("User is already part of this group.");

                await _usersCollection.UpdateOneAsync(
                    u => u.Id == userId,
                    Builders<UserModel>.Update.AddToSet(u => u.GroupIds, groupId)
                );

                _logger.LogInformation("Successfully added group ID {GroupId} to user {UserId}.", groupId, userId);
                return new SuccessDataResult<ObjectId>("Group with the ID saved successfully.", groupId);
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while adding the group.");
                return new ErrorResult("An error occurred while adding the group.", ErrorType.ServerError);
            }
        }

        public async Task<ResultModel> AddSuperAdminMessageId(string superId, ObjectId messageId)
        {
            try
            {
                var superAdmin = await _superadminCollection.Find(s => s.UserId == superId).FirstOrDefaultAsync();
                if (superAdmin == null)
                    return new ErrorResult("Superadmin not found.", ErrorType.NotFound);
                
                if (!superAdmin.BroadcastedIds.Contains(messageId))
                {
                    superAdmin.BroadcastedIds.Add(messageId);
                    var updateDefinition = Builders<Superadmin>.Update.Set(s => s.BroadcastedIds, superAdmin.BroadcastedIds);
                    await _superadminCollection.UpdateOneAsync(s => s.UserId == superId, updateDefinition);

                    return new SuccessResult("Message ID added to broadcast list.");
                }
                else
                {
                    return new ErrorResult("Message ID is already in the broadcasted list.", ErrorType.Conflict);
                }
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while adding the broadcast message ID.");
                return new ErrorResult("An error occurred while adding the broadcast message ID.", ErrorType.ServerError);
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

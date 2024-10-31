using Amazon.Runtime.Internal;
using MongoDB.Bson;
using MongoDB.Driver;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Interface;
using RealTimeChatApp.API.Models;
using RealTimeChatApp.API.ViewModels.RequestModels;
using RealTimeChatApp.API.ViewModels.ResultModels;
using System.Linq;
using System.Text.RegularExpressions;

namespace RealTimeChatApp.API.Repository
{
    public class GroupRepository : IGroupRepository
    {
        private readonly IMongoCollection<GroupModel> _groupCollection;
        private readonly IMongoCollection<UserModel> _userCollection;
        private readonly ILogger<GroupRepository> _logger;

        public GroupRepository(IMongoDatabase mongoDb, ILogger<GroupRepository> logger)
        {
            _groupCollection = mongoDb.GetCollection<GroupModel>("groups");
            _userCollection = mongoDb.GetCollection<UserModel>("users");
            _logger = logger;
        }
        public async Task<ResultModel> CheckUserRoleInGroupChat(string userId, ObjectId chatId)
        {
            try
            {
                var group = await _groupCollection.Find(g => g.GroupChat.Id == chatId)
                                                   .FirstOrDefaultAsync();
                if (group == null)
                    return new ErrorResult("Group not found.");

                bool isAdmin = group.AdminId == userId;
                bool isMember = group.GroupChat.ParticipantIds.Contains(userId);

                return new SuccessDataResult<(bool isAdmin, bool isMember)>("User role in group fetched.", (isAdmin, isMember));
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while checking the user role in the group.");
                return new ErrorResult("An error occurred while checking the user role in the group.");
            }
        }

        public async Task<ResultModel> GetGroupById(ObjectId id)
        {
            try
            {
                var group = await _groupCollection.Find(g => g.Id == id).FirstOrDefaultAsync();
                return group != null
                    ? new SuccessDataResult<GroupModel>("Group found and fetched", group)
                    : new ErrorResult("Group not found");
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the requested group.");
                return new ErrorResult("An error occurred while fetching the requested group.");
            }
        }
        public async Task<ResultModel> GetGroupDetails(List<ObjectId> groupIds)
        {
            var groupDetailsList = new List<GroupDetailsResponse>();
            if (groupIds == null || groupIds.Count == 0)
                return new SuccessDataResult<List<GroupDetailsResponse>>("No group is requested.", groupDetailsList);
            try
            {
                var groupDetailsResponses = await Task.WhenAll(groupIds.Select(async groupId =>
                {
                    var groupResult = await GetGroupById(groupId);
                    if (groupResult is SuccessDataResult<GroupModel> group)
                    {
                        var gData = group.Data;
                        return new GroupDetailsResponse
                        {
                            GroupId = gData.Id.ToString(),
                            GroupName = gData.GroupName,
                            Description = gData.Description,
                            GroupChatId = gData.GroupChat.Id.ToString(),
                            LastMessageContent = gData.GroupChat.LastMessageContent,
                            LastMessageSender = gData.GroupChat.LastMessageSenderFullname
                        };
                    }
                    return null;
                }));
                groupDetailsList = groupDetailsResponses.Where(g => g != null).ToList();    // filter out the nulls
                return new SuccessDataResult<List<GroupDetailsResponse>>("Successfully fetched the groups' details.", groupDetailsList);
            }
            catch (MongoException mongoEx)
            {
                _logger.LogError(mongoEx, "A MongoDB error occurred while fetching the requested groups' details.");
                return new ErrorResult("A database error occurred while fetching the requested groups' details.");
            }
            catch (AggregateException aggEx)
            {
                foreach (var ex in aggEx.InnerExceptions)
                {
                    _logger.LogError(ex, "An error occurred in one of the tasks while fetching the requested groups' details.");
                }
                return new ErrorResult("One or more errors occurred while fetching the requested groups' details.");
            }
        }

        public async Task<ResultModel> GetGroupMemberIds(ObjectId groupChatId)
        {
            try
            {
                var memberIds = await _groupCollection.Find(g => g.GroupChat.Id == groupChatId)
                    .Project(g => g.GroupChat.ParticipantIds)
                    .FirstOrDefaultAsync();

                if (memberIds == null || memberIds.Count == 0)
                    return new ErrorResult("Group not found or has no members.");
                
                return new SuccessDataResult<List<string>>("Fetched the member IDs of group.", memberIds);
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the group member IDs.");
                return new ErrorResult("An error occurred while fetching the group member IDs.");
            }
        }

        public async Task<ResultModel> GetGroupMemberDetails(ObjectId groupChatId)
        {
            try
            {
                var memberIdsResult = await GetGroupMemberIds(groupChatId);
                if (memberIdsResult is SuccessDataResult<List<string>> memberIds)
                {
                    var members = await _userCollection
                        .Find(m => memberIds.Data.Contains(m.Id))
                        .ToListAsync();

                    var memberDetailsList = members.Select(member => new MemberDetailsResponse
                    {
                        MemberId = member.Id,
                        Fullname = member.FullName,
                        StatusMessage = member.StatusMessage,
                        MemberPictureUrl = member.ProfilePictureUrl,
                        IsOnline = member.IsOnline
                    }).ToList();
                    return new SuccessDataResult<List<MemberDetailsResponse>>("Group member details fetched successfully", memberDetailsList);
                }
                else
                    return new ErrorResult("Failed to fetch member details of group");
                
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the group members' details.");
                return new ErrorResult("An error occurred while fetching the group members' details.");
            }
        }


        public async Task<ResultModel> GetGroupChat(ObjectId chatId)
        {
            try
            {
                var projection = Builders<GroupModel>.Projection.Expression(g => g.GroupChat);

                var groupChat = await _groupCollection.Find(g => g.GroupChat.Id == chatId)
                    .Project<ChatModel>(projection)
                    .FirstOrDefaultAsync();

                return groupChat != null
                    ? new SuccessDataResult<ChatModel>("Fetched the group chat successfully.", groupChat)
                    : new ErrorResult("Error while fetching the chat for given ID.");
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the group chat");
                return new ErrorResult("An error occurred while fetching the group chat.");
            }
        }

        public async Task<ResultModel> GetGroupsChatIds(List<ObjectId> groupIds)
        {
            try
            {
                if (groupIds == null || !groupIds.Any())
                    return new ErrorResult("No group IDs provided.");

                var groupChatIds = await _groupCollection
                    .Find(g => groupIds.Contains(g.Id))
                    .Project(g => g.GroupChat.Id)
                    .ToListAsync();

                return new SuccessDataResult<List<ObjectId>>("Fetched group chat IDs successfully.", groupChatIds);
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the groups' chat IDs");
                return new ErrorResult("An error occurred while fetching the groups' chat IDs.");
            }
        }

        public async Task<ResultModel> GetGroupMessageIds(ObjectId groupId)
        {
            try
            {
                var messageIds = await _groupCollection.Find(g => g.Id == groupId)
                    .Project(g => g.GroupChat.MessageIds)
                    .FirstOrDefaultAsync();

                return messageIds != null
                    ? new SuccessDataResult<List<ObjectId>>("Fetched the group message IDs successfully.", messageIds)
                    : new ErrorResult("Error while fetching the messages for given ID.");
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the group message IDs");
                return new ErrorResult("An error occurred while fetching the group message IDs.");
            }
        }

        public async Task<ResultModel> CreateNewGroup(string adminId, AddGroupRequest request)
        {
            try
            {
                var newGroup = new GroupModel(adminId, request.GroupName, request.MemberIds, request.Description);
                await _groupCollection.InsertOneAsync(newGroup);
                return new SuccessDataResult<ObjectId>("Group with the ID created.", newGroup.Id);
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while creating the group");
                return new ErrorResult("An error occurred while creating the group");
            }
        }

        public async Task<ResultModel> AddMemberToGroup(ObjectId groupId, string newMemberId)
        {
            try
            {
                var group = await _groupCollection
                    .Find(g => g.Id == groupId)
                    .Project(g => new { g.GroupChat.ParticipantIds })
                    .FirstOrDefaultAsync();

                if (group == null)
                    return new ErrorResult("Group not found.");

                if (group.ParticipantIds.Contains(newMemberId))
                    return new ErrorResult("The member is already part of the group.");

                var filter = Builders<GroupModel>.Filter.Eq(g => g.Id, groupId);
                var update = Builders<GroupModel>.Update.AddToSet(g => g.GroupChat.ParticipantIds, newMemberId);

                var addResult = await _groupCollection.UpdateOneAsync(filter, update);

                return addResult.ModifiedCount > 0
                    ? new SuccessResult("Member added successfully.")
                    : new ErrorResult("Failed to add the member for given IDs.");
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while adding a member to group.");
                return new ErrorResult("An error occurred while adding a member to group.");
            }
        }

        public async Task<ResultModel> SaveMessageToGroupChat(ObjectId groupChatId, MessageModel newMessage)
        {
            try
            {
                var messageIds = await _groupCollection
                    .Find(g => g.GroupChat.Id == groupChatId)
                    .Project(g => g.GroupChat.MessageIds)
                    .FirstOrDefaultAsync();

                if (messageIds == null)
                    return new ErrorResult("No messages found.");

                if (messageIds.Contains(newMessage.Id))
                    return new ErrorResult("This message is already saved in group chat.");

                var filter = Builders<GroupModel>.Filter.Eq(g => g.GroupChat.Id, groupChatId);
                var update = Builders<GroupModel>.Update.AddToSet(g => g.GroupChat.MessageIds, newMessage.Id)
                                                        .Set(g => g.GroupChat.LastMessageSenderFullname, newMessage.SenderFullname)
                                                        .Set(g => g.GroupChat.LastMessageContent, newMessage.Content);

                var addResult = await _groupCollection.UpdateOneAsync(filter, update);
                return addResult.ModifiedCount > 0
                    ? new SuccessResult("Message saved successfully.")
                    : new ErrorResult("Failed to save the message for given IDs.");
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while saving the message to group.");
                return new ErrorResult("An error occurred while saving the message to group.");
            }
        }

        public async Task<ResultModel> UpdateGroupDetails(UpdateGroupRequest request)
        {
            try
            {
                if (!ObjectId.TryParse(request.GroupId, out ObjectId objectId))
                    return new ErrorResult("Error while parsing groupId");

                var filter = Builders<GroupModel>.Filter.Eq(g => g.Id, objectId);
                var update = Builders<GroupModel>.Update
                    .Set(g => g.GroupName, request.GroupName)
                    .Set(g => g.Description, request.Description);

                var updateResult = await _groupCollection.UpdateOneAsync(filter, update);
                return updateResult.ModifiedCount > 0
                    ? new SuccessResult("Group details updated successfully")
                    : new ErrorResult("Group not found or no changes were made.");
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while updating group details.");
                return new ErrorResult("An error occurred while updating group details.");
            }
        }

        public async Task<ResultModel> ChangeAdminOfGroup(ObjectId groupId, string newAdminId)
        {
            try
            {
                var filter = Builders<GroupModel>.Filter.Eq(g => g.Id, groupId);
                var update = Builders<GroupModel>.Update
                    .Set(g => g.AdminId, newAdminId);

                var updateResult = await _groupCollection.UpdateOneAsync(filter, update);
                return updateResult.ModifiedCount > 0
                    ? new SuccessResult("Group admin changed successfully")
                    : new ErrorResult("Group not found or no changes were made.");
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while changing group admin.");
                return new ErrorResult("An error occurred while changing group admin.");
            }
        }

        public async Task<ResultModel> KickMemberFromGroup(ObjectId groupId, string memberId)
        {
            try
            {
                var filter = Builders<GroupModel>.Filter.Eq(g => g.Id, groupId);
                var update = Builders<GroupModel>.Update.Pull(g => g.GroupChat.ParticipantIds, memberId);

                var updateResult = await _groupCollection.UpdateOneAsync(filter, update);
                return updateResult.ModifiedCount > 0
                    ? new SuccessResult("Member kicked out successfully.")
                    : new ErrorResult("Group not found or member not present in group.");
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while kicking the member from the group.");
                return new ErrorResult("An error occurred while kicking the member from the group.");
            }
        }

        public async Task<ResultModel> RemoveMessageFromGroupChat(ObjectId groupId, ObjectId messageId)
        {
            try
            {
                var filter = Builders<GroupModel>.Filter.Eq(g => g.Id, groupId);
                var update = Builders<GroupModel>.Update.Pull(g => g.GroupChat.MessageIds, messageId);

                var updateResult = await _groupCollection.UpdateOneAsync(filter, update);
                return updateResult.ModifiedCount > 0
                    ? new SuccessResult("Message removed successfully.")
                    : new ErrorResult("Group not found or message not present in group chat.");
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while removing the message from the group.");
                return new ErrorResult("An error occurred while removing message from the group.");
            }
        }

        public async Task<ResultModel> DeleteGroup(ObjectId groupId)
        {
            try
            {
                var deleteResult = await _groupCollection.DeleteOneAsync(g => g.Id == groupId);
                return deleteResult.DeletedCount > 0
                    ? new SuccessResult("Group deleted successfully.")
                    : new ErrorResult("Failed to delete the group.");
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "An error occurred while removing the group.");
                return new ErrorResult("An error occurred while removing the group.");
            }
        }
    }
}
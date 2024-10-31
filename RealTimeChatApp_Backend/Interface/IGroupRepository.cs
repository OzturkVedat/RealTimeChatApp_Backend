using MongoDB.Bson;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Models;
using RealTimeChatApp.API.ViewModels.RequestModels;

namespace RealTimeChatApp.API.Interface
{
    public interface IGroupRepository
    {
        Task<ResultModel> CheckUserRoleInGroupChat(string userId, ObjectId chatId);
        Task<ResultModel> GetGroupById(ObjectId groupId);
        Task<ResultModel> GetGroupDetails(List<ObjectId> groupIds);
        Task<ResultModel> GetGroupMemberIds(ObjectId groupChatId);
        Task<ResultModel> GetGroupMemberDetails(ObjectId groupChatId);
        Task<ResultModel> GetGroupChat(ObjectId chatId);
        Task<ResultModel> GetGroupsChatIds(List<ObjectId> groupIds);
        Task<ResultModel> GetGroupMessageIds(ObjectId groupId);
        Task<ResultModel> CreateNewGroup(string adminId, AddGroupRequest request);
        Task<ResultModel> AddMemberToGroup(ObjectId groupId, string newMemberId);
        Task<ResultModel> SaveMessageToGroupChat(ObjectId groupChatId, MessageModel newMessage);
        Task<ResultModel> UpdateGroupDetails(UpdateGroupRequest request);
        Task<ResultModel> ChangeAdminOfGroup(ObjectId groupId, string newAdminId);
        Task<ResultModel> KickMemberFromGroup(ObjectId groupId, string memberId);
        Task<ResultModel> RemoveMessageFromGroupChat(ObjectId groupId, ObjectId messageId);
        Task<ResultModel> DeleteGroup(ObjectId groupId);
    }
}

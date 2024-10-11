using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Interface;
using RealTimeChatApp.API.Models;

namespace RealTimeChatApp.API.Hubs
{
    [Authorize(AuthenticationSchemes =JwtBearerDefaults.AuthenticationScheme)]
    public class NotificationHub : Hub
    {
        private readonly IChatRepository _chatRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMessageRepository _messageRepository;
        public NotificationHub(IChatRepository chatRepository, IUserRepository userRepository, IMessageRepository messageRepository)
        {
            _chatRepository = chatRepository;
            _userRepository = userRepository;
            _messageRepository = messageRepository;
        }

        public async Task GetAuthenticatedUserId()
        {
            var userId = Context.UserIdentifier;
            if (userId == null)
            {
                await Clients.Caller.SendAsync("ReceiveErrorMessage", "User not authenticated.");
                return;
            }
            await Clients.Caller.SendAsync("ReceiveAuthenticatedUserId", userId);
        }

        public async Task GetFriendsOnlineStatus()
        {
            var userId = Context.UserIdentifier;
            if (userId == null)
            {
                await Clients.Caller.SendAsync("ReceiveErrorMessage", "User not authenticated.");
                return;
            }
            var friendIdsResult = await _userRepository.GetUserFriendIds(userId);
            if (friendIdsResult is SuccessDataResult<List<string>> idsSuccessResult)
            {
                var onlineStatusResult = await _userRepository.GetUserFriendsOnlineStatus(idsSuccessResult.Data);
                if (onlineStatusResult is SuccessDataResult<Dictionary<string, bool>> onlineStatusSuccess)
                {
                    var onlineStatus = onlineStatusSuccess.Data;
                    await Clients.Caller.SendAsync("ReceiveFriendsOnlineStatus", onlineStatus); // send the dictionary
                }
                else await Clients.Caller.SendAsync("ReceiveErrorMessage", onlineStatusResult.Message);
            }
            else await Clients.Caller.SendAsync("ReceiveErrorMessage", friendIdsResult.Message);
        }
        public async Task SendTypingNotification(ObjectId chatId)
        {
            var chatResult = await _chatRepository.GetChatById(chatId);
            if (chatResult.IsSuccess)
            {
                await Clients.Group(chatId.ToString()).SendAsync("ReceiveTypingNotification", Context.UserIdentifier);
            }
        }

        public async Task MarkPrivateAsRead(ObjectId messageId, string recipentId)
        {
            var idList = new List<ObjectId> { messageId };
            var messageResult = await _messageRepository.GetPrivateMessagesByIds(idList);
            if (messageResult is SuccessDataResult<List<PrivateMessage>> successResult)
            {
                if (!successResult.Data.Any())
                {
                    await Clients.Caller.SendAsync("ReceiveErrorMessage", "Message not found.");
                    return;
                }
                var message = successResult.Data.First();
                if (message.RecipientId == recipentId && !message.ReadStatus)
                {
                    message.ReadStatus = true;
                    await _messageRepository.UpdatePrivateMessage(message);

                    // Notify the sender that the message has been read
                    await Clients.User(message.SenderId).SendAsync("ReceivePrivateMessageRead", messageId.ToString(), recipentId);
                }
            }
            else
                await Clients.Caller.SendAsync("ReceiveErrorMessage", messageResult.Message);

        }

        public async Task MarkGroupAsRead(ObjectId messageId, string userId)
        {
            var idList = new List<ObjectId> { messageId };
            var messageResult = await _messageRepository.GetGroupMessagesByIds(idList);
            if (messageResult is SuccessDataResult<List<GroupMessage>> successResult)
            {
                if (!successResult.Data.Any())
                {
                    await Clients.Caller.SendAsync("ReceiveErrorMessage", "Message not found.");
                    return;
                }
                var message = successResult.Data.First();
                if (message.RecipientIds.Contains(userId) && message.ReadStatus.ContainsKey(userId) && !message.ReadStatus[userId])
                {
                    message.ReadStatus[userId] = true;
                    await _messageRepository.UpdateGroupMessage(message);

                    // Notify all users in the group that the message has been read by this user
                    await Clients.Group(message.Id.ToString()).SendAsync("ReceiveGroupMessageRead", messageId.ToString(), userId);
                }
            }
            else
                await Clients.Caller.SendAsync("ReceiveErrorMessage", messageResult.Message);

        }
    }
}

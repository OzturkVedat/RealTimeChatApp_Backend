using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Interface;
using RealTimeChatApp.API.Models;
using RealTimeChatApp.API.Repository;
using System.Security.Claims;

namespace RealTimeChatApp.API.Hubs
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class GroupHub : Hub
    {
        private readonly IUserRepository _userRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly UserManager<UserModel> _userManager;
        private readonly ILogger<GroupHub> _logger;

        public GroupHub(IUserRepository userRepository, IGroupRepository groupRepository,
            IMessageRepository messageRepository, UserManager<UserModel> userManager, ILogger<GroupHub> logger)
        {
            _userRepository = userRepository;
            _groupRepository = groupRepository;
            _messageRepository = messageRepository;
            _userManager = userManager;
            _logger = logger;
        }
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            _logger.LogInformation($"User {userId} connected.");

            if (userId == null)
            {
                _logger.LogWarning("User not authenticated.");
                await SendErrorToCaller("User not authenticated.");
                return;
            }

            // Log fetching group IDs for the user
            var userGroupIds = await _userRepository.GetUserIdsByType(userId, "groupIds");
            if (userGroupIds is SuccessDataResult<List<ObjectId>> groupIds)
            {
                _logger.LogInformation($"Fetched {groupIds.Data.Count} group IDs for user {userId}.");

                // Log fetching chat room IDs for the user's groups
                var userGroupChatIds = await _groupRepository.GetGroupsChatIds(groupIds.Data);
                if (userGroupChatIds is SuccessDataResult<List<ObjectId>> chatIds)
                {
                    _logger.LogInformation($"Fetched {chatIds.Data.Count} chat room IDs for user {userId}.");

                    // Attempt to join all chat rooms
                    var tasks = chatIds.Data.Select(chatId => JoinChatRoom(chatId.ToString())).ToArray();
                    await Task.WhenAll(tasks);

                    _logger.LogInformation($"User {userId} joined {chatIds.Data.Count} chat rooms.");
                }
                else
                {
                    _logger.LogError($"Error while connecting chat rooms for user {userId}.");
                    await SendErrorToCaller("Error while connecting chat rooms.");
                }
            }
            else
            {
                _logger.LogError($"Error while retrieving group IDs for user {userId}.");
                await SendErrorToCaller("Error while connecting chat rooms.");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            _logger.LogInformation($"User {userId} disconnected. Reason: {exception?.Message}");

            if (userId == null)
            {
                _logger.LogWarning("User not authenticated.");
                await SendErrorToCaller("User not authenticated.");
                return;
            }

            // Log fetching group IDs for the user
            var userGroupIds = await _userRepository.GetUserIdsByType(userId, "groupIds");
            if (userGroupIds is SuccessDataResult<List<ObjectId>> groupIds)
            {
                _logger.LogInformation($"Fetched {groupIds.Data.Count} group IDs for user {userId}.");

                // Log fetching chat room IDs for the user's groups
                var userGroupChatIds = await _groupRepository.GetGroupsChatIds(groupIds.Data);
                if (userGroupChatIds is SuccessDataResult<List<ObjectId>> chatIds)
                {
                    _logger.LogInformation($"Fetched {chatIds.Data.Count} chat room IDs for user {userId}.");

                    // Attempt to leave all chat rooms
                    var tasks = chatIds.Data.Select(chatId => LeaveChatRoom(chatId.ToString())).ToArray();
                    await Task.WhenAll(tasks);

                    _logger.LogInformation($"User {userId} left {chatIds.Data.Count} chat rooms.");
                }
                else
                {
                    _logger.LogError($"Error while disconnecting chat rooms for user {userId}.");
                    await SendErrorToCaller("Error while disconnecting.");
                }
            }
            else
            {
                _logger.LogError($"Error while retrieving group IDs for user {userId}.");
                await SendErrorToCaller("Error while disconnecting chat rooms.");
            }

            await base.OnDisconnectedAsync(exception);
        }
        public async Task JoinChatRoom(string chatId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
            return;
        }

        public async Task LeaveChatRoom(string chatId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId);
            return;
        }

        public async Task GetGroupChatMessages(string chatId)
        {
            if (!ObjectId.TryParse(chatId, out ObjectId objectId))
            {
                await SendErrorToCaller("Wrong chat ID format");
                return;
            }

            if (await CheckUserRoleAndProceed(objectId))        // check if user is a member/admin of group
            {
                var chatResult = await _groupRepository.GetGroupChat(objectId);
                if (!chatResult.IsSuccess)
                {
                    var errorResult = (ErrorResult)chatResult;
                    await SendErrorToCaller(errorResult.Message);
                }
                if (chatResult is SuccessDataResult<ChatModel> chatSuccess)
                {
                    var chat = chatSuccess.Data;
                    if (!chat.ParticipantIds.Contains(Context.UserIdentifier))
                        await SendErrorToCaller("You are not a participant in this chat.");

                    var messagesResult = await _messageRepository.GetMessagesByIds(chat.MessageIds);
                    if (messagesResult is SuccessDataResult<List<MessageModel>> successResult)
                    {
                        await Clients.Caller.SendAsync("ReceivePreviousGroupMessages", successResult.Data);
                        return;
                    }
                    await SendErrorToCaller("Failed to retrieve messages.");
                }
            }
                
        }

        public async Task SendGroupMessage(string chatId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                await SendErrorToCaller("Message cannot be empty.");
                return;
            }

            if (!ObjectId.TryParse(chatId, out ObjectId objectId))
            {
                await SendErrorToCaller("Invalid chat ID format");
                return;
            }
            if (await CheckUserRoleAndProceed(objectId))
            {
                var groupChatResult = await _groupRepository.GetGroupChat(objectId);
                if (groupChatResult is SuccessDataResult<GroupModel> success)
                {
                    var userId = Context.UserIdentifier;
                    if (!success.Data.GroupChat.ParticipantIds.Contains(userId))
                        await SendErrorToCaller("You are not a participant in this chat.");
                    var user = await _userManager.FindByIdAsync(userId);
                    var newMessage = new MessageModel(userId, user.FullName, success.Data.GroupChat.ParticipantIds, message);
                    var saveResult = await _messageRepository.SaveNewMessage(newMessage);
                    if (saveResult.IsSuccess)
                    {
                        var addResult = await _groupRepository.SaveMessageToGroupChat(objectId, newMessage);
                        if (addResult.IsSuccess)
                        {
                            var messageResult = await _messageRepository.GetMessageById(newMessage.Id);
                            if (messageResult is SuccessDataResult<MessageModel> messageSuccess)
                                await Clients.Group(chatId).SendAsync("ReceiveMessage", messageSuccess.Data);
                            else
                                await SendErrorToCaller("Failed to fetch the message details.");
                        }
                        else
                            await SendErrorToCaller("Error while saving the message to chat.");
                    }
                    else
                        await SendErrorToCaller("Error while saving the message to database.");
                }
                else
                    await SendErrorToCaller("Error while fething group chat.");
            }
                
        }

        public async Task SendGroupTypingNotification(string groupChatId)
        {
            if (!ObjectId.TryParse(groupChatId, out ObjectId objectId))
            {
                await SendErrorToCaller("Invalid chat ID format");
                return;
            }
            if (await CheckUserRoleAndProceed(objectId))
            {
                var chatResult = await _groupRepository.GetGroupChat(objectId);
                if (chatResult.IsSuccess)
                {
                    await Clients.Group(groupChatId).SendAsync("ReceiveGroupTypingNotification", Context.UserIdentifier);
                    return;
                }
                else
                    await SendErrorToCaller("Error while sending typing notification.");
            }
                

        }
        private async Task SendErrorToCaller(string errorMessage)
        {
            await Clients.Caller.SendAsync("ReceiveErrorMessage", errorMessage);
            return;
        }

        private async Task<bool> CheckUserRoleAndProceed(ObjectId groupId)
        {
            var userId = Context.UserIdentifier;
            if (userId == null)
            {
                await SendErrorToCaller("User not authenticated.");
                return false;
            }
            var userRoleResult = await _groupRepository.CheckUserRoleInGroup(userId, groupId);
            if (userRoleResult is SuccessDataResult<(bool, bool)> roleCheck)
            {
                var (isAdmin, isMember) = roleCheck.Data;
                if (isAdmin || isMember)
                    return true;
                else
                {
                    await SendErrorToCaller("User is not authorized to perform this action.");
                    return false;
                }
            }
            else
            {
                await SendErrorToCaller("Error checking user role.");
                return false;
            }
        }
    }
}

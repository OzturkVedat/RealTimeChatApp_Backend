using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Interface;
using RealTimeChatApp.API.Models;
using RealTimeChatApp.API.Services;
using RealTimeChatApp.API.ViewModels.ResultModels;

namespace RealTimeChatApp.API.Hubs
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ChatHub : Hub
    {
        private readonly IUserRepository _userRepository;
        private readonly IChatRepository _chatRepository;
        private readonly IMessageRepository _messageRepository;

        public ChatHub(IUserRepository userRepository, IChatRepository chatRepository,
            IMessageRepository messageRepository)
        {
            _userRepository = userRepository;
            _chatRepository = chatRepository;
            _messageRepository = messageRepository;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (userId == null)
            {
                await SendErrorToCaller("User not authenticated.");
                
                return;
            }
            var userChatIds = await _userRepository.GetUserIdsByType(userId, "chatIds");
            if (userChatIds is SuccessDataResult<List<ObjectId>> successResult)
            {
                // join each chat for comm
                var tasks = successResult.Data.Select(chatId => JoinChatRoom(chatId.ToString())).ToArray();
                await Task.WhenAll(tasks);  // parallel async execution
            }
            else
                await SendErrorToCaller("Error fetching user chats while connecting to chat hub.");

            var friendIdsResult = await _userRepository.GetUserIdsByType(userId, "friendIds");
            if (friendIdsResult is SuccessDataResult<List<string>> userFriendIds)
                await SendOnlineStatus(userFriendIds.Data, true);
            else
                await SendErrorToCaller("Error while sending online status to friends.");

            await base.OnConnectedAsync();      // ensure base logic executed
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (userId == null)
            {
                await SendErrorToCaller("User not authenticated.");
                
                return;
            }

            var updateResult = await _userRepository.UpdateUserStatus(userId, false);
            var userChatIds = await _userRepository.GetUserIdsByType(userId, "chatIds");
            if (updateResult.IsSuccess && userChatIds is SuccessDataResult<List<ObjectId>> successResult)
            {
                var tasks = successResult.Data.Select(chatId => LeaveChatRoom(chatId.ToString())).ToArray();
                await Task.WhenAll(tasks);
            }
            else
                await SendErrorToCaller("Error while disconnecting chat rooms.");

            var friendIdsResult = await _userRepository.GetUserIdsByType(userId, "friendIds");
            if (friendIdsResult is SuccessDataResult<List<string>> userFriendIds)
                await SendOnlineStatus(userFriendIds.Data, false);
            else
                await SendErrorToCaller("Error while sending online status to friends.");
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

        public async Task GetChatMessages(string chatId)        // for private chat
        {
            var userId = Context.UserIdentifier;
            if (userId == null)
            {
                await SendErrorToCaller("User not authenticated.");
                
                return;
            }
            if (!ObjectId.TryParse(chatId, out ObjectId objectId))
                await SendErrorToCaller("Wrong chat ID format");

            var chatResult = await _chatRepository.GetPrivateChatById(objectId);
            if (!chatResult.IsSuccess)
            {
                var errorResult = (ErrorResult)chatResult;
                await SendErrorToCaller(errorResult.Message);
            }
            if (chatResult is SuccessDataResult<ChatModel> chatSuccess)
            {
                var chat = chatSuccess.Data;
                if (!chat.ParticipantIds.Contains(userId))
                    await SendErrorToCaller("You are not a participant in this chat.");

                var messagesResult = await _messageRepository.GetMessagesByIds(chat.MessageIds);
                if (messagesResult is SuccessDataResult<List<MessageModel>> successResult)
                {
                    await Clients.Caller.SendAsync("ReceivePreviousMessages", successResult.Data);
                    return;
                }
                await SendErrorToCaller("Failed to retrieve messages.");
            }
        }

        public async Task SendMessage(string chatId, string message)    // for private chats
        {
            if (string.IsNullOrWhiteSpace(message))
                await SendErrorToCaller("Message cannot be empty.");

            if (!ObjectId.TryParse(chatId, out ObjectId objectId))
                await SendErrorToCaller("Invalid chat ID format");

            var userId = Context.UserIdentifier;
            var userResult = await _userRepository.GetUserById(userId);
            if (userId == null || !userResult.IsSuccess)
            {
                await SendErrorToCaller("User not authenticated.");
                
                return;
            }
            var user = (SuccessDataResult<UserModel>)userResult;

            var chatResult = await _chatRepository.GetPrivateChatById(objectId);
            if (!chatResult.IsSuccess)
            {
                var errorResult = (ErrorResult)chatResult;
                await SendErrorToCaller(errorResult.Message);
            }
            if (chatResult is SuccessDataResult<ChatModel> chatSuccess)
            {
                var chat = chatSuccess.Data;
                if (!chat.ParticipantIds.Contains(userId))
                    await SendErrorToCaller("You are not a participant in this chat.");

                var idList = chat.ParticipantIds.Where(id => id != userId).ToList();     // get everyone aside from the user

                var newMessage = new MessageModel(userId, user.Data.FullName, idList, message);
                var saveResult = await _messageRepository.SaveNewMessage(newMessage);
                if (!saveResult.IsSuccess)
                    await SendErrorToCaller("Failed to save the message.");

                var addResult = await _chatRepository.AddMessageToPrivateChat(objectId, newMessage);
                if (!addResult.IsSuccess)
                    await SendErrorToCaller("Failed to update chat with the new message.");

                var result = await _messageRepository.GetMessageById(newMessage.Id);
                if (idList.Count == 1)  // Private chat
                {
                    var recipientId = idList.FirstOrDefault();
                    if (recipientId != null && result is SuccessDataResult<MessageModel> success)
                    {
                        var details = success.Data;
                        await Clients.User(userId).SendAsync("ReceiveMessage", details);        // send it to sender too, for smoother chat
                        if (userId != recipientId)      // in case user sends himself a message
                            await Clients.User(recipientId).SendAsync("ReceiveMessage", details);
                        return;
                    }
                }
                else
                    await SendErrorToCaller("Chat state is invalid.");
            }
        }


        public async Task BroadcastMessage(string message)
        {
            var userId = Context.UserIdentifier;
            if (userId == null)
            {
                await SendErrorToCaller("User not authenticated.");
                
                return;
            }
            if (string.IsNullOrWhiteSpace(message))
                await SendErrorToCaller("Message cannot be empty.");
            await Clients.All.SendAsync("ReceiveBroadcast", message);
        }

        public async Task SendTypingNotification(string chatId)     // for private chats 
        {
            var userId = Context.UserIdentifier;
            if (userId == null)
            {
                await SendErrorToCaller("User not authenticated.");
                
                return;
            }

            if (!ObjectId.TryParse(chatId, out ObjectId objectId))
                await SendErrorToCaller("Invalid chat ID format");

            var chatResult = await _chatRepository.GetPrivateChatById(objectId);
            if (chatResult.IsSuccess)
            {
                await Clients.Group(chatId).SendAsync("ReceiveTypingNotification", userId);
                return;
            }
            else
                await SendErrorToCaller("Error while sending typing notification.");
        }

        private async Task SendOnlineStatus(List<string> friendIds, bool isOnline)
        {
            var userId = Context.UserIdentifier;
            if (userId == null)
            {
                await SendErrorToCaller("User not authenticated.");
                
            }

            var onlineStatus = new OnlineStatusResponse
            {
                UserId = userId,
                IsOnline = isOnline
            };
            var tasks = friendIds.Select(friendId => Clients.User(friendId).SendAsync("ReceiveOnlineStatus", onlineStatus));
            await Task.WhenAll(tasks);
        }

        private async Task SendErrorToCaller(string errorMessage)
        {
            await Clients.Caller.SendAsync("ReceiveErrorMessage", errorMessage);
            return;
        }
    }
}


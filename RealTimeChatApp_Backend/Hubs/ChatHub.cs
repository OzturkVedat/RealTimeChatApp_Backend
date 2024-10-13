using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Interface;
using RealTimeChatApp.API.Models;
using RealTimeChatApp.API.ViewModels.ResultModels;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ChatHub : Hub
{
    private readonly IUserRepository _userRepository;
    private readonly IChatRepository _chatRepository;
    private readonly IMessageRepository _messageRepository;

    public ChatHub(IUserRepository userRepository, IChatRepository chatRepository, IMessageRepository messageRepository)
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
            await Clients.Caller.SendAsync("ReceiveErrorMessage", "User not authenticated.");
            return;
        }
        await _userRepository.UpdateUserStatus(userId, true);

        var userChatIds = await _userRepository.GetUserChatIds(userId);
        if (userChatIds is SuccessDataResult<List<string>> successResult)
        {
            foreach (var chatId in successResult.Data)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, chatId);     // join each chat for comm
            }
        }
        else
        {
            await Clients.Caller.SendAsync("ReceiveErrorMessage", "Error fetching user chats.");
        }
        await base.OnConnectedAsync();      // ensure base logic executed
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (userId == null)
        {
            await Clients.Caller.SendAsync("ReceiveErrorMessage", "User not authenticated.");
            return;
        }

        await _userRepository.UpdateUserStatus(userId, false);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinChatRoom(string chatId)
    {
        if (!ObjectId.TryParse(chatId, out ObjectId objectId))
        {
            await Clients.Caller.SendAsync("ReceiveErrorMessage", "Wrong chat ID format");
            return;
        }

        var chatResult = await _chatRepository.GetChatById(objectId);
        if (chatResult.IsSuccess)
        {
            if (chatResult is SuccessDataResult<ChatModel> successResult)
            {
                var chatIdString = successResult.Data.Id.ToString();
                await Groups.AddToGroupAsync(Context.ConnectionId, chatIdString);
            }
        }
        else
        {
            if (chatResult is ErrorResult errorResult)      // send error message to client
                await Clients.Caller.SendAsync("ReceiveErrorMessage", errorResult.Message);
            return;
        }
    }
    public async Task LeaveChatRoom(string chatId)
    {
        if (!ObjectId.TryParse(chatId, out ObjectId objectId))
        {
            await Clients.Caller.SendAsync("ReceiveErrorMessage", "Wrong chat ID format");
            return;
        }
        var chatResult = await _chatRepository.GetChatById(objectId);
        if (chatResult.IsSuccess)
        {
            if (chatResult is SuccessDataResult<ChatModel> successResult)
            {
                var chatIdString = successResult.Data.Id.ToString();
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatIdString);
            }
        }
        else
        {
            if (chatResult is ErrorResult errorResult)
                await Clients.Caller.SendAsync("ReceiveErrorMessage", errorResult.Message);
            return;
        }
    }

    public async Task GetChatMessages(string chatId)
    {
        var userId = Context.UserIdentifier;
        if (userId == null)
        {
            await Clients.Caller.SendAsync("ReceiveErrorMessage", "User not authenticated.");
            return;
        }
        if (!ObjectId.TryParse(chatId, out ObjectId objectId))
        {
            await Clients.Caller.SendAsync("ReceiveErrorMessage", "Invalid chat ID format.");
            return;
        }

        var chatResult = await _chatRepository.GetChatById(objectId);
        if (!chatResult.IsSuccess)
        {
            var errorResult = (ErrorResult)chatResult;
            await Clients.Caller.SendAsync("ReceiveErrorMessage", errorResult.Message);
            return;
        }
        if (chatResult is SuccessDataResult<ChatModel> chatSuccess)
        {
            var chat = chatSuccess.Data;
            if (!chat.ParicipantIds.Contains(userId))
            {
                await Clients.Caller.SendAsync("ReceiveErrorMessage", "You are not a participant in this chat.");
                return;
            }

            var messagesResult = await _messageRepository.GetMessageDetailsAsync(chat.MessageIds);
            if (messagesResult is SuccessDataResult<List<MessageDetailsRespone>> successResult)
            {
                await Clients.Caller.SendAsync("ReceivePreviousMessages", successResult.Data);
                return;
            }
            await Clients.Caller.SendAsync("ReceiveErrorMessage", "Failed to retrieve messages.");
        }
    }

    public async Task SendMessage(string chatId, string message)
    {
        var userId = Context.UserIdentifier;
        if (userId == null)
        {
            await Clients.Caller.SendAsync("ReceiveErrorMessage", "User not authenticated.");
            return;
        }
        if (string.IsNullOrWhiteSpace(message))
        {
            await Clients.Caller.SendAsync("ReceiveErrorMessage", "Message cannot be empty.");
            return;
        }
        if (!ObjectId.TryParse(chatId, out ObjectId objectId))
        {
            await Clients.Caller.SendAsync("ReceiveErrorMessage", "Invalid chat ID format.");
            return;
        }
        var chatResult = await _chatRepository.GetChatById(objectId);
        if (!chatResult.IsSuccess)
        {
            var errorResult = (ErrorResult)chatResult;
            await Clients.Caller.SendAsync("ReceiveErrorMessage", errorResult.Message);
            return;
        }
        if (chatResult is SuccessDataResult<ChatModel> chatSuccess)
        {
            var chat = chatSuccess.Data;
            if (!chat.ParicipantIds.Contains(userId))
            {
                await Clients.Caller.SendAsync("ReceiveErrorMessage", "You are not a participant in this chat.");
                return;
            }
            var idList = chat.ParicipantIds.Where(id => id != userId).ToList();     // get everyone aside from the user

            var newMessage = new MessageModel(userId, idList, message);
            var saveResult = await _messageRepository.SaveNewMessage(newMessage);
            if (!saveResult.IsSuccess)
            {
                await Clients.Caller.SendAsync("ReceiveErrorMessage", "Failed to save the message.");
                return;
            }
            var addResult = await _chatRepository.AddMessageToChat(objectId, newMessage);
            if (!addResult.IsSuccess)
            {
                await Clients.Caller.SendAsync("ReceiveErrorMessage", "Failed to update chat with the new message.");
                return;
            }
            var result = await _messageRepository.GetMessageDetailsAsync(new List<ObjectId> { newMessage.Id });
            if (idList.Count == 1)  // Private chat
            {
                var recipientId = idList.FirstOrDefault();
                if (recipientId != null && result is SuccessDataResult<List<MessageDetailsRespone>> success)
                {
                    var details = success.Data.FirstOrDefault();
                    await Clients.User(recipientId).SendAsync("ReceiveMessage", details);  // Send to recipient only
                    return;
                }
            }
            else  // Group chat
            {
                if (result is SuccessDataResult<List<MessageDetailsRespone>> success)
                {
                    var details = success.Data.FirstOrDefault();
                    await Clients.Group(chatId).SendAsync("ReceiveMessage", details);  // Send to the entire group
                    return;
                }

            }
        }
    }

}

//Clients.Caller: Refers to the client that is currently making the request.
//Clients.All: Sends a message to all connected clients(broadcast).
//Clients.Client(connectionId): Sends a message to a specific client identified by their connection ID.
//Clients.User(userId): Sends a message to a specific user identified by their user ID (usually based on authenticated user info).
//Clients.Group(groupName): Sends a message to all clients in a specific group.

// Groups.AddToGroupAsync(Context.ConnectionId, "chatRoom1"): Adding a user to a group (e.g., chat room)
// Groups.RemoveFromGroupAsync(Context.ConnectionId, "chatRoom1"): Removing a user from a group
// Clients.Group("chatRoom1").SendAsync("ReceiveMessage", "Hello, chatRoom1!"): Send a message to all users in the group "chatRoom1"
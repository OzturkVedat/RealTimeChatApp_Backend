using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Interface;
using RealTimeChatApp.API.Models;

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

    public async Task JoinChatRoom(ObjectId chatId)
    {
        var chatResult = await _chatRepository.GetChatById(chatId);
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
    public async Task LeaveChatRoom(ObjectId chatId)
    {
        var chatResult = await _chatRepository.GetChatById(chatId);
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

    public async Task SendMessage(ObjectId chatId, string userId, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            await Clients.Caller.SendAsync("ReceiveErrorMessage", "Message cannot be empty.");
            return;
        }
        var chatResult = await _chatRepository.GetChatById(chatId);
        if (!chatResult.IsSuccess)
        {
            var errorResult = (ErrorResult)chatResult;
            await Clients.Caller.SendAsync("ReceiveErrorMessage", errorResult.Message);
            return;
        }
        await Clients.Group(chatId.ToString()).SendAsync("ReceiveMessage", userId, message);
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

//Clients.Caller: Refers to the client that is currently making the request.
//Clients.All: Sends a message to all connected clients(broadcast).
//Clients.Client(connectionId): Sends a message to a specific client identified by their connection ID.
//Clients.User(userId): Sends a message to a specific user identified by their user ID (usually based on authenticated user info).
//Clients.Group(groupName): Sends a message to all clients in a specific group.

// Groups.AddToGroupAsync(Context.ConnectionId, "chatRoom1"): Adding a user to a group (e.g., chat room)
// Groups.RemoveFromGroupAsync(Context.ConnectionId, "chatRoom1"): Removing a user from a group
// Clients.Group("chatRoom1").SendAsync("ReceiveMessage", "Hello, chatRoom1!"): Send a message to all users in the group "chatRoom1"
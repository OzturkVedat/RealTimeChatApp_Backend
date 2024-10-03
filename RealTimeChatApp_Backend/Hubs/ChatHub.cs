using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;

using RealTimeChatApp.API.Services;
using RealTimeChatApp.API.Models;

namespace RealTimeChatApp.API.Hubs
{
    public sealed class ChatHub : Hub
    {
        private readonly IMongoCollection<MessageModel> _messagesCollection;
        private readonly HubHelperService _helperService;
        private readonly UserManager<UserModel> _userManager;

        public ChatHub(IMongoDatabase mongoDb,HubHelperService helperService, UserManager<UserModel> userManager)
        {
            _messagesCollection = mongoDb.GetCollection<MessageModel>("messages");
            _helperService = helperService;
            _userManager = userManager;
        }

        //Clients.Caller: Refers to the client that is currently making the request.
        //Clients.All: Sends a message to all connected clients(broadcast).
        //Clients.Client(connectionId): Sends a message to a specific client identified by their connection ID.
        //Clients.User(userId): Sends a message to a specific user identified by their user ID (usually based on authenticated user info).
        //Clients.Group(groupName): Sends a message to all clients in a specific group.

        // Groups.AddToGroupAsync(Context.ConnectionId, "chatRoom1"): Adding a user to a group (e.g., chat room)
        // Groups.RemoveFromGroupAsync(Context.ConnectionId, "chatRoom1"): Removing a user from a group
        // Clients.Group("chatRoom1").SendAsync("ReceiveMessage", "Hello, chatRoom1!"): Send a message to all users in the group "chatRoom1"

        public async Task SendPrivateMessage(string recipentUserId, string messageContent)
        {
            var senderUser = await _userManager.GetUserAsync(Context.User);
            if (senderUser == null)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "You are not authenticated.");
                return;
            }


            var message = new MessageModel
            {
                SenderId = senderUser.Id,
                RecipientIds = new List<string> { recipentUserId },  // private message, only one recipent
                Content = messageContent,
                SentAt = DateTime.UtcNow,
                ReadStatus = new Dictionary<string, bool>
                {
                    {recipentUserId, false}
                }
            };
            await _messagesCollection.InsertOneAsync(message);

            await Clients.User(recipentUserId).SendAsync("ReceiveMessage", senderUser.FullName, message.Content, message.SentAt);
        }
        public async Task SendGroupMessage(string groupId, string messageContent)
        {
            // Get the sender user from the SignalR context (assuming the user is logged in)
            var senderUser = await _userManager.GetUserAsync(Context.User);
            if (senderUser == null)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "You are not authenticated.");
                return;
            }
            
            var roomUsers = await _helperService.GetUserIdsInGroupAsync(groupId);

            // Create a new message model
            var message = new MessageModel
            {
                SenderId = senderUser.Id,
                RecipientIds = roomUsers,
                Content = messageContent,
                SentAt = DateTime.UtcNow,
                ReadStatus = roomUsers.ToDictionary(userId => userId, userId => false)  // set all to false
            };

            await _messagesCollection.InsertOneAsync(message);
            await Clients.Group(groupId).SendAsync("ReceiveGroupMessage", senderUser.FullName, message.Content, message.SentAt);
        }
        public async Task JoinChat(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await Clients.Group(roomId).SendAsync("UserJoined", Context.ConnectionId);
        }

        public async Task LeaveChat(string roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            await Clients.Group(roomId).SendAsync("UserLeft", Context.ConnectionId);
        }

        public async Task GetMessageHistory(string recipientUserId)
        {
            var senderUser = await _userManager.GetUserAsync(Context.User);
            if (senderUser == null)
            {
                return;
            }

            // Fetch messages where the sender is either the current user or the recipient is in the recipientUserId list
            var messages = await _messagesCollection
                .Find(m => m.SenderId == senderUser.Id || m.RecipientIds.Contains(recipientUserId))
                .SortByDescending(m => m.SentAt)
                .Limit(50)  // Fetch only the last 50 messages
                .ToListAsync();

            // Send the message history to the calling user
            await Clients.Caller.SendAsync("ReceiveMessageHistory", messages);
        }
    }
}

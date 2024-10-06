using Microsoft.AspNetCore.SignalR;

public class ChatHub :Hub
{

}

//Clients.Caller: Refers to the client that is currently making the request.
//Clients.All: Sends a message to all connected clients(broadcast).
//Clients.Client(connectionId): Sends a message to a specific client identified by their connection ID.
//Clients.User(userId): Sends a message to a specific user identified by their user ID (usually based on authenticated user info).
//Clients.Group(groupName): Sends a message to all clients in a specific group.

// Groups.AddToGroupAsync(Context.ConnectionId, "chatRoom1"): Adding a user to a group (e.g., chat room)
// Groups.RemoveFromGroupAsync(Context.ConnectionId, "chatRoom1"): Removing a user from a group
// Clients.Group("chatRoom1").SendAsync("ReceiveMessage", "Hello, chatRoom1!"): Send a message to all users in the group "chatRoom1"
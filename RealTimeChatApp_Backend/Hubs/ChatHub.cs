using Microsoft.AspNetCore.SignalR;

namespace RealTimeChatApp.API.Hubs
{
    public sealed class ChatHub: Hub
    {
        public override Task OnConnectedAsync()
        {
            throw new NotImplementedException();     
        }

        public async Task SendPrivateMessage(string userId, string message)
        {

        }
    }
}

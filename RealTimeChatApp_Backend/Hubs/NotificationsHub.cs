using Microsoft.AspNetCore.SignalR;

namespace RealTimeChatApp.API.Hubs
{
    public class NotificationsHub:Hub<INotificationClient>
    {

        public override async Task OnConnectedAsync()
        {
            throw new NotImplementedException();
        }
    }
    public interface INotificationClient
    {
        Task ReceiveNotification(string message);
    }
}

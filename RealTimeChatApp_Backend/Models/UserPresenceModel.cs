namespace RealTimeChatApp.API.Models
{
    public class UserPresenceModel
    {
        public string UserId { get; set; }
        public bool IsOnline {  get; set; }
        public DateTime LastSeenAt { get; set; }= DateTime.Now;
        public List<string> ConnectionIds { get; set; } // List of SignalR connection IDs
    }
}

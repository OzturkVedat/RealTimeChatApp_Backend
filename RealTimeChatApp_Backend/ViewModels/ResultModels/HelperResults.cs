using MongoDB.Bson;

namespace RealTimeChatApp.API.ViewModels.ResultModels
{
    public class LoginResponse
    {
        public string FullName { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
    public class ChatDetailsResponse
    {
        public string ChatId { get; set; }
        public string LastMessage { get; set; }
        public string LastMessageSender { get; set; }
        public string RecipientFullname { get; set; }
    }

    public class MemberDetailsResponse
    {
        public string MemberId { get; set; }
        public string Fullname { get; set; }
        public string StatusMessage { get; set; }
        public bool IsOnline { get; set; }
    }

    public class GroupDetailsResponse
    {
        public string GroupId { get; set; }
        public string GroupName { get; set; }
        public string? Description { get; set; }
        public string LastMessageContent { get; set; }
        public string LastMessageSender { get; set; }
    }

    public class OnlineStatusResponse
    {
        public string UserId { get; set; }
        public bool IsOnline { get; set; }
    }
}

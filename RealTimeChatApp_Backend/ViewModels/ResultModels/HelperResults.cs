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
        public string RecipientPictureUrl { get; set; }
    }
    public class UserDetailsResponse
    {
        public string Id { get; set; }
        public string Fullname {  get; set; }
        public string Email {  get; set; }
        public string ProfilePicUrl { get; set; }
        public bool IsOnline {  get; set; }

    }
    public class FriendDetailsResponse
    {
        public string Id { get; set; }
        public string Fullname { get; set; }
        public string PictureUrl { get; set; }
        public string StatusMessage { get; set; }
        public bool IsOnline { get; set; }
    }
    public class MemberDetailsResponse
    {
        public string MemberId { get; set; }
        public string Fullname { get; set; }
        public string StatusMessage { get; set; }
        public string MemberPictureUrl { get; set; }
        public bool IsOnline { get; set; }
    }
    public class SearchDetailsResponse
    {
        public string UserId { get; set; }
        public string Fullname { get; set; }
        public string UserPictureUrl { get; set; }

    }
    public class GroupDetailsResponse
    {
        public string GroupId { get; set; }
        public string GroupName { get; set; }
        public string? Description { get; set; }
        public string GroupChatId { get; set; }
        public string LastMessageContent { get; set; }
        public string LastMessageSender { get; set; }
    }

    public class OnlineStatusResponse
    {
        public string UserId { get; set; }
        public bool IsOnline { get; set; }
    }
}

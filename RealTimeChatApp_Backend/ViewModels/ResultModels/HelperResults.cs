using MongoDB.Bson;

namespace RealTimeChatApp.API.ViewModels.ResultModels
{
    public class ChatDetailsResponse
    {
        public string ChatId { get; set; }
        public string LastMessage { get; set; }
        public string RecipientFullname { get; set; }
    }
    public class MessageDetailsRespone
    {
        public string MessageId { get; set; }
        public string SenderFullname { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public bool ReadStatus {  get; set; }
    }
}

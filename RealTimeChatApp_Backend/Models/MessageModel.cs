using MongoDB.Bson;
using MongoDbGenericRepository.Attributes;
using System.Security.Cryptography;
using System.Text;

namespace RealTimeChatApp.API.Models
{
    [CollectionName("messages")]
    public class MessageModel
    {
        public ObjectId Id { get; set; }
        public string SenderId { get; set; }
        public string SenderFullname { get; set; }
        public string Content { get; set; }
        public string ContentHash { get; set; }  
        public DateTime SentAt { get; set; }

        public MessageModel(string senderId, string senderFullname, string content)
        {
            SenderId = senderId;
            SenderFullname = senderFullname;
            Content = content;
            ContentHash = ComputeHash(content);  
            SentAt = DateTime.UtcNow;
        }
        private string ComputeHash(string content)          // SHA256 hashing
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(content));
                StringBuilder builder = new StringBuilder();

                foreach (byte b in bytes)       // convert byte to hexadecimal
                {
                    builder.Append(b.ToString("x2"));       // accumulate
                }
                return builder.ToString();
            }
        }
    }
}

﻿using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using MongoDbGenericRepository.Attributes;

namespace RealTimeChatApp.API.Models
{
    [CollectionName("chats")]
    public class ChatModel
    {
        public ObjectId Id { get; set; }
        public List<string> ParicipantIds { get; set; } 
        public string ChatTitle {  get; set; }
        public ObjectId LastMessageId { get; set; } 
        public ChatType Type { get; set; }
        public List<ObjectId> MessageIds { get; set; }

        public ChatModel(string chatTitle, List<string> userIds)
        {
            ChatTitle = chatTitle;
            ParicipantIds = userIds;
            Type = userIds.Count > 2 ? ChatType.Group : ChatType.Private;     // determine type based on number of users
            MessageIds = new List<ObjectId>();
        }
    }
    public enum ChatType
    {
        Private,
        Group
    }

}

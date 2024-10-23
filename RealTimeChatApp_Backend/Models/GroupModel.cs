using MongoDB.Bson;
using MongoDbGenericRepository.Attributes;

namespace RealTimeChatApp.API.Models
{
    [CollectionName("groups")]
    public class GroupModel
    {
        public ObjectId Id { get; set; }
        public string GroupName { get; set; }
        public string Description { get; set; }
        public string AdminId { get; set; }
        public ChatModel GroupChat { get; set; }

        public GroupModel() { }
        public GroupModel(string createdById, string groupName, List<string> memberIds, string description = "")
        {
            AdminId = createdById;
            GroupName = groupName;
            Description = description;
            GroupChat = new ChatModel(memberIds);
        }
    }

}

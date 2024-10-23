//using Amazon.Runtime.Internal;
//using MongoDB.Bson;
//using MongoDB.Driver;
//using RealTimeChatApp.API.Models;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace RealTimeChatApp.API.Data
//{
//    public class Seeder
//    {
//        private readonly IMongoCollection<UserModel> _userCollection;
//        private readonly IMongoCollection<ChatModel> _chatCollection;
//        private readonly IMongoCollection<MessageModel> _messageCollection;
//        private readonly IMongoCollection<GroupModel> _groupCollection;

//        public Seeder(IMongoDatabase mongoDb)
//        {
//            _userCollection = mongoDb.GetCollection<UserModel>("users");
//            _chatCollection = mongoDb.GetCollection<ChatModel>("chats");
//            _messageCollection = mongoDb.GetCollection<MessageModel>("messages");
//            _groupCollection = mongoDb.GetCollection<GroupModel>("groups");
//        }

//        public async Task SeedAsync()
//        {
//            // Seed Users
//            if (!await _userCollection.AsQueryable().AnyAsync())
//            {
//                var users = new List<UserModel>
//                {
//                    new() { Id = "user1", FullName = "Kim Jung", Email = "jungkim@email.com",UserName="jungkim@email.com",
//                        StatusMessage = "Hello there!", IsOnline = false },
//                    new () { Id = "user2", FullName = "Miranda Keller", Email = "mirandak@email.com",UserName= "mirandak@email.com",
//                        StatusMessage = "Hello there!", IsOnline = false },
//                    new() { Id = "user3", FullName = "Jean Grouchy", Email = "grouchy@email.com",UserName="grouchy@email.com",
//                        StatusMessage = "Hello there!", IsOnline = false },
//                    new() { Id = "user4", FullName = "Sam Watson", Email = "samwat@email.com",UserName="samwat@email.com",
//                        StatusMessage = "Hello there!", IsOnline = false },
//                    new() { Id = "user5", FullName = "Marie Nova", Email = "mnova@email.com",UserName="mnova@email.com",
//                        StatusMessage = "Hello there!", IsOnline = false },
//                    new() { Id = "user6", FullName = "Van Damm", Email = "dammvan@email.com",UserName="dammvan@email.com",
//                        StatusMessage = "Hello there!", IsOnline = false },
//                    new() { Id = "user7", FullName = "Ed Almeida", Email = "almeida@email.com",UserName="almeida@email.com",
//                        StatusMessage = "Hello there!", IsOnline = false },
//                };

//                await _userCollection.InsertManyAsync(users);
//            }

//            if (!await _chatCollection.AsQueryable().AnyAsync())
//            {
//                var chats = new List<ChatModel>
//                {
//                    new (["user1","user2"]),
//                    new(["user1","user3"]),
//                    new(["user1","user5"]),
//                    new(["user7","user6"]),
//                    new(["user5","user2"]),
//                    new(["user1","user5","user7","user3","user2"])  // group chat
//                };

//                await _chatCollection.InsertManyAsync(chats);
//            }

//            if (!await _messageCollection.AsQueryable().AnyAsync())
//            {
//                var messages = new List<MessageModel>
//                {
//                    new ("user1",["user2"],"dont back up now"),
//                    new ("user1",["user3"],"im coming"),
//                    new ("user1",["user5"],"sure"),
//                    new ("user7",["user6"],"yep"),
//                    new ("user5",["user2"],"exactly")
//                };

//                await _messageCollection.InsertManyAsync(messages);
//            }

//            if (!await _groupCollection.AsQueryable().AnyAsync())
//            {
          
//                var groups = new List<GroupModel>
//                {
//                    new GroupModel { GroupName = "Developers",Description= "School project", AdminId = "user1",
//                    GroupChat()},
//                    // Add more groups as needed
//                };

//                await _groupCollection.InsertManyAsync(groups);
//            }
//        }
//    }
//}

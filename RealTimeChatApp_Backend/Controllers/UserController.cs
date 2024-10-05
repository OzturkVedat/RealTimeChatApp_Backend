using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Interface;
using RealTimeChatApp.API.Models;
using RealTimeChatApp.API.ViewModels.RequestModels;
using System.Security.Claims;

namespace RealTimeChatApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IChatRepository _chatRepository;
        private readonly IMessageRepository _messageRepository;

        public UserController(IUserRepository userRepository, IChatRepository chatRepository, IMessageRepository messageRepository)
        {
            _userRepository = userRepository;
            _chatRepository = chatRepository;
            _messageRepository = messageRepository;
        }

        [HttpGet("user-details")]
        public async Task<IActionResult> GetAuthenticatedUserDetails()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new ErrorResult("User not authenticated."));
            var user = await _userRepository.GetUserById(userIdClaim.Value);
            if (user == null) return NotFound(new ErrorResult("User not found!?"));

            return Ok(new SuccessDataResult<UserModel>("Successfully fetched the authenticated user details.", user));
        }

        [HttpGet("user-private-chats")]
        public async Task<IActionResult> GetUserLastPrivateChatDetails([FromQuery] int limit)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new ErrorResult("User not authenticated."));
            var chatIds = await _userRepository.GetLastUserChatsById(userIdClaim.Value, limit);
            var chats = await _chatRepository.GetChatsByIds(chatIds);
            return Ok(new SuccessDataResult<List<ChatModel>>($"Successfully fetched the last {limit} chats.", chats));
        }

        [HttpGet("chat-messages")]
        public async Task<IActionResult> GetMessagesOfChatById(ObjectId chatId)
        {
            var chat = await _chatRepository.GetChatById(chatId);
            if (chat == null) return NotFound(new ErrorResult("Chat not found."));

            if (chat.Type == ChatType.Private)
            {
                var messages = await _messageRepository.GetPrivateMessagesByIds(chat.MessageIds);
                return Ok(new SuccessDataResult<List<PrivateMessage>>("Successfully fetched the private chat messages.", messages));
            }
            else
            {
                var messages = await _messageRepository.GetGroupMessagesByIds(chat.MessageIds);
                return Ok(new SuccessDataResult<List<GroupMessage>>("Successfully fetched the group chat messages.", messages));
            }
        }

        [HttpGet("friend-details")]
        public async Task<IActionResult> GetFriendsDetails()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new ErrorResult("User not authenticated."));

            var friendlistIds = await _userRepository.GetUserFriendIds(userIdClaim.Value);
            if (friendlistIds == null)
                return NotFound(new ErrorResult("Error while fetching friendlist"));

            var userFullnames = await _userRepository.GetUserFriendFullnames(friendlistIds);
            return Ok(new SuccessDataResult<Dictionary<string, string>>("Successfully fetched the friend's fullnames.", userFullnames));
        }

        [HttpPost("add-friend")]
        public async Task<IActionResult> AddToFriendListByEmail([FromBody] AddFriendRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorDataResult("Invalid input.", ModelState.GetErrors()));

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new ErrorResult("User not authenticated."));

            var result = await _userRepository.SaveUserFriendByEmail(userIdClaim.Value, request.Email);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("new-private-chat")]
        public async Task<IActionResult> CreateNewPrivateChat([FromBody] NewChatRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new ErrorResult("User not authenticated."));
            var chatters = new List<string> { userIdClaim.Value, request.FriendId };
            var newChat = new ChatModel(request.ChatTitle, chatters);
            await _chatRepository.SaveChat(newChat);
            await _userRepository.SaveUserChatById(userIdClaim.Value, newChat.Id);
            return Ok(new SuccessResult("New chat created."));
        }

    }
}

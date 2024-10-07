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

            var result = await _userRepository.GetUserById(userIdClaim.Value);
            if (result is ErrorResult errorResult)
            {
                if (errorResult.Type == ErrorType.NotFound)
                    return NotFound(errorResult);

                return BadRequest(errorResult);     // server-side error
            }
            return Ok(result);
        }

        [HttpGet("user-private-chats/{limit}")]
        public async Task<IActionResult> GetUserLastPrivateChatDetails(int limit)
        {
            if (limit <= 0)
                return BadRequest(new ErrorResult("Limit must be a positive integer."));

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new ErrorResult("User not authenticated."));

            var chatIdsResult = await _userRepository.GetLastUserChatsById(userIdClaim.Value, limit);
            if( chatIdsResult is SuccessDataResult<List<ObjectId>> successResult)
            {
                var chatsResult = await _chatRepository.GetChatsByIds(successResult.Data);
                return chatsResult.IsSuccess ? Ok(chatsResult) : BadRequest(chatsResult);
            }
            return BadRequest(chatIdsResult);    

        }

        [HttpGet("chat-messages")]
        public async Task<IActionResult> GetMessagesOfChatById(ObjectId chatId)
        {
            var chatResult = await _chatRepository.GetChatById(chatId);
            if (chatResult is ErrorResult errorResult)
            {
                if (errorResult.Type == ErrorType.NotFound)
                    return NotFound(errorResult);
                return BadRequest(errorResult);
            }
            var successResult = chatResult as SuccessDataResult<ChatModel>;
            if (successResult.Data.Type == ChatType.Private)
            {
                var messages = await _messageRepository.GetPrivateMessagesByIds(successResult.Data.MessageIds);
                return messages.IsSuccess ? Ok(messages) : BadRequest(messages);
            }
            else
            {
                var messages = await _messageRepository.GetGroupMessagesByIds(successResult.Data.MessageIds);
                return messages.IsSuccess ? Ok(messages) : BadRequest(messages);
            }
        }

        [HttpGet("friend-fullnames")]
        public async Task<IActionResult> GetFriendsFullnames()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new ErrorResult("User not authenticated."));

            var friendIdsResult = await _userRepository.GetUserFriendIds(userIdClaim.Value);
            if (!friendIdsResult.IsSuccess)
            {
                var errorResult = friendIdsResult as ErrorResult;
                if (errorResult.Type == ErrorType.NotFound)
                    return NotFound(errorResult);
                return BadRequest(errorResult);
            }
            var successResult = friendIdsResult as SuccessDataResult<List<string>>;
            var friendNamesResult = await _userRepository.GetUserFriendFullnames(successResult.Data);
            return friendNamesResult.IsSuccess ? Ok(friendNamesResult) : BadRequest(friendNamesResult);
        }

        [HttpPost("add-friend")]
        public async Task<IActionResult> AddToFriendListByEmail([FromBody] AddFriendRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorDataResult("Invalid input.", ModelState.GetErrors()));

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new ErrorResult("User not authenticated."));

            var addFriendResult = await _userRepository.AddUserFriendByEmail(userIdClaim.Value, request.Email);
            var errorResult = addFriendResult as ErrorResult;    // cast to check whether it is an error
            if (errorResult != null)
            {
                if (errorResult.Type == ErrorType.NotFound) return NotFound(errorResult);
                if (errorResult.Type == ErrorType.Conflict) return Conflict(errorResult);
                return BadRequest(errorResult);
            }
            return Ok(addFriendResult);
        }

        [HttpPost("new-private-chat")]
        public async Task<IActionResult> CreateNewPrivateChat([FromBody] NewChatRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new ErrorResult("User not authenticated."));

            var chatters = new List<string> { userIdClaim.Value, request.FriendId };
            var newChat = new ChatModel(request.ChatTitle, chatters);

            var saveChatResult = await _chatRepository.SaveChat(newChat);
            if (!saveChatResult.IsSuccess)
                return BadRequest(saveChatResult);

            var chatAddResult = await _userRepository.AddUserChatById(userIdClaim.Value, newChat.Id);
            var errorResult = chatAddResult as ErrorResult;
            if (errorResult != null)
            {
                if (errorResult.Type == ErrorType.NotFound) return NotFound(errorResult);
                return BadRequest(errorResult);
            }
            return Ok(chatAddResult);
        }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Interface;
using RealTimeChatApp.API.Models;
using RealTimeChatApp.API.ViewModels.RequestModels;
using RealTimeChatApp.API.ViewModels.ResultModels;
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

        [HttpGet("user-private-chats")]
        public async Task<IActionResult> GetUserPrivateChatDetails()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new ErrorResult("User not authenticated."));

            var chatIdsResult = await _userRepository.GetUserChatIds(userIdClaim.Value);
            if (chatIdsResult is SuccessDataResult<List<ObjectId>> successResult)
            {
                var detailsList = new List<ChatDetailsResponse>();
                var tasks = successResult.Data.Select(async chatId =>
                {
                    var result = await _chatRepository.GetChatDetailsAsync(chatId, userIdClaim.Value);
                    if (result is SuccessDataResult<ChatDetailsResponse> successDetails)
                    {
                        detailsList.Add(successDetails.Data);
                    }
                });
                await Task.WhenAll(tasks);
                return Ok(new SuccessDataResult<List<ChatDetailsResponse>>("Successfully fetched the details.", detailsList));
            }
            return BadRequest(chatIdsResult);
        }

        [HttpGet("friend-fullnames")]
        public async Task<IActionResult> GetFriendsFullnames()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new ErrorResult("User not authenticated."));

            var friendIdsResult = await _userRepository.GetUserFriendIds(userIdClaim.Value);
            if (friendIdsResult is SuccessDataResult<List<string>> successResult)
            {
                var friendNamesResult = await _userRepository.GetUserFriendsFullnames(successResult.Data);
                return friendNamesResult.IsSuccess ? Ok(friendNamesResult) : BadRequest(friendNamesResult);
            }
            else
            {
                var errorResult = friendIdsResult as ErrorResult;
                if (errorResult.Type == ErrorType.NotFound)
                    return NotFound(errorResult);
                return BadRequest(errorResult);
            }
        }

        [HttpGet("friend-online-status")]
        public async Task<IActionResult> GetFriendOnlineStatus()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new ErrorResult("User not authenticated."));

            var friendIdsResult = await _userRepository.GetUserFriendIds(userIdClaim.Value);
            if (friendIdsResult is SuccessDataResult<List<string>> successResult)
            {
                var friendStatus = await _userRepository.GetUserFriendsOnlineStatus(successResult.Data);
                return friendStatus.IsSuccess ? Ok(friendStatus) : BadRequest(friendStatus);
            }
            else
            {
                var errorResult = friendIdsResult as ErrorResult;
                if (errorResult.Type == ErrorType.NotFound)
                    return NotFound(errorResult);
                return BadRequest(errorResult);
            }
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
            if (addFriendResult is ErrorResult errorResult)
            {
                if (errorResult.Type == ErrorType.NotFound) return NotFound(errorResult);
                if (errorResult.Type == ErrorType.Conflict) return Conflict(errorResult);
                return BadRequest(errorResult);
            }
            if (addFriendResult is SuccessDataResult<string> friendIdResult)
            {
                var chatters = new List<string> { userIdClaim.Value, friendIdResult.Data };
                var newChat = new ChatModel(chatters);      // create a new chat with this friend right away
                return await CreateNewPrivateChat(newChat);
            }
            return BadRequest(new ErrorResult("Unexpected error occurred."));  // Fallback error handling
        }

        private async Task<IActionResult> CreateNewPrivateChat(ChatModel chat)
        {
            var saveChatResult = await _chatRepository.SaveChat(chat);
            if (!saveChatResult.IsSuccess)
                return BadRequest(saveChatResult);

            var addChatResults = new List<ResultModel>();
            foreach (var id in chat.ParicipantIds)
            {
                var result = await _userRepository.AddUserChatById(id, chat.Id);
                addChatResults.Add(result);
            }
            foreach (var result in addChatResults)
            {
                if (result is ErrorResult errorResult)
                {
                    if (errorResult.Type == ErrorType.NotFound)
                        return NotFound(errorResult);
                    return BadRequest(errorResult); // Handle other errors
                }
            }
            return Ok(new SuccessResult("Chat added successfully."));
        }

    }
}

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

        [HttpGet("user-id")]
        public IActionResult GetAuthenticatedUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new ErrorResult("User not authenticated."));
            }
            return Ok(new SuccessDataResult<string>("User ID successfully retrieved.", userIdClaim.Value));
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

            var chatIdsResult = await _userRepository.GetUserIdsByType(userIdClaim.Value, "chatIds");
            if (chatIdsResult is SuccessDataResult<List<ObjectId>> successResult)
            {
                if (successResult.Data == null || successResult.Data.Count == 0)
                    return Ok(new SuccessDataResult<List<ChatDetailsResponse>>("No chats found.", new List<ChatDetailsResponse>()));

                var chatDetailsTasks = successResult.Data.Select(chatId =>
                _chatRepository.GetPrivateChatDetails(chatId, userIdClaim.Value)).ToList();
                var chatDetailsResults = await Task.WhenAll(chatDetailsTasks);

                var detailsList = chatDetailsResults
                                     .OfType<SuccessDataResult<ChatDetailsResponse>>()
                                     .Select(success => success.Data)
                                     .ToList();
                return Ok(new SuccessDataResult<List<ChatDetailsResponse>>("Successfully fetched the details.", detailsList));
            }
            return BadRequest(chatIdsResult);
        }

        [HttpGet("friend-details")]
        public async Task<IActionResult> GetUserFriendsDetails()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new ErrorResult("User not authenticated."));

            var friendIdsResult = await _userRepository.GetUserIdsByType(userIdClaim.Value, "friendIds");
            if (friendIdsResult is SuccessDataResult<List<string>> successResult)
            {
                var friendNamesResult = await _userRepository.GetUserFriendsDetails(successResult.Data);
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
        
        [HttpPost("add-friend")]
        public async Task<IActionResult> AddToFriendListById([FromBody] AddFriendRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorDataResult("Invalid input.", ModelState.GetErrors()));

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new ErrorResult("User not authenticated."));

            var addFriendResult = await _userRepository.AddUserFriendById(userIdClaim.Value, request.FriendId);
            if (addFriendResult is ErrorResult errorResult)
            {
                if (errorResult.Type == ErrorType.NotFound) return NotFound(errorResult);
                if (errorResult.Type == ErrorType.Conflict) return Conflict(errorResult);
                return BadRequest(errorResult);
            }
            else
            {
                var chatters = new List<string> { userIdClaim.Value, request.FriendId };
                var newChat = new ChatModel(chatters);      // create a new chat with this friend right away
                return await CreateNewPrivateChat(newChat);
            }
        }

        private async Task<IActionResult> CreateNewPrivateChat(ChatModel chat)
        {
            var saveChatResult = await _chatRepository.SavePrivateChat(chat);
            if (!saveChatResult.IsSuccess)
                return BadRequest(saveChatResult);

            var addChatResults = new List<ResultModel>();
            foreach (var id in chat.ParticipantIds)
            {
                var result = await _userRepository.AddUserPrivateChatById(id, chat.Id);
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
            return Ok(new SuccessResult("Friend added and a chat created for this user."));
        }
    }
}

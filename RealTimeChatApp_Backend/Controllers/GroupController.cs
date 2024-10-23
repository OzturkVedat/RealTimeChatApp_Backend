using Amazon.Runtime.Internal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Interface;
using RealTimeChatApp.API.Models;
using RealTimeChatApp.API.ViewModels.RequestModels;
using RealTimeChatApp.API.ViewModels.ResultModels;
using System.Data;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace RealTimeChatApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroupController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly UserManager<UserModel> _userManager;

        public GroupController(IUserRepository userRepository, IGroupRepository groupRepository, UserManager<UserModel> userManager)
        {
            _userRepository = userRepository;
            _groupRepository = groupRepository;
            _userManager = userManager;
        }
        // delegate functions for authentication
        private async Task<IActionResult> CheckUserRoleAndProceed(ObjectId groupId, Func<Task<IActionResult>> action)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new ErrorResult("User not authenticated."));

            var userRoleResult = await _groupRepository.CheckUserRoleInGroup(userIdClaim.Value, groupId);
            if (userRoleResult is SuccessDataResult<(bool, bool)> roleCheck)
            {
                var (isAdmin, isMember) = roleCheck.Data;
                if (isAdmin || isMember)
                    return await action();
                else
                    return Forbid();
            }
            else return BadRequest(userRoleResult);
        }

        private async Task<IActionResult> CheckAdminAndProceed(ObjectId groupId, Func<Task<IActionResult>> action)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new ErrorResult("User not authenticated."));

            var userRoleResult = await _groupRepository.CheckUserRoleInGroup(userIdClaim.Value, groupId);
            if (userRoleResult is SuccessDataResult<(bool isAdmin, bool isMember)> roleCheck)
            {
                if (roleCheck.Data.isAdmin)
                    return await action();
                else
                    return Forbid();
            }
            return BadRequest(userRoleResult);
        }

        [HttpGet("{groupId}/details")]
        public async Task<IActionResult> GetGroupDetails([FromRoute] string groupId)
        {
            if (!ObjectId.TryParse(groupId, out ObjectId objectId))
                return BadRequest(new ErrorResult("Invalid groupId format."));

            return await CheckUserRoleAndProceed(objectId, async () =>
            {
                var detailsResult = await _groupRepository.GetGroupById(objectId);
                return detailsResult.IsSuccess ? Ok(detailsResult) : BadRequest(detailsResult);
            });
        }

        [HttpGet("{groupId}/members")]
        public async Task<IActionResult> GetGroupMembersDetails([FromRoute] string groupId)
        {
            if (!ObjectId.TryParse(groupId, out ObjectId objectId))
                return BadRequest(new ErrorResult("Invalid groupId format."));

            return await CheckUserRoleAndProceed(objectId, async () =>
            {
                var detailsResult = await _groupRepository.GetGroupMemberDetails(objectId);
                return detailsResult.IsSuccess ? Ok(detailsResult) : BadRequest(detailsResult);
            });
        }

        [HttpGet("all-groups")]
        public async Task<IActionResult> GetAllJoinedGroupDetails()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new ErrorResult("User not authenticated."));
            var groupIdsResult = await _userRepository.GetUserIdsByType(userIdClaim.Value, "groupIds");
            if (groupIdsResult is SuccessDataResult<List<ObjectId>> groupIds)
            {
                var detailResult = await _groupRepository.GetGroupDetails(groupIds.Data);
                return detailResult.IsSuccess ? Ok(detailResult) : BadRequest(detailResult);
            }
            return BadRequest(groupIdsResult);  // get the error from the repository
        }

        [HttpGet("group-member-fullnames")]
        public async Task<IActionResult> GetGroupMembersFullnames([FromRoute] string groupId)
        {
            if (!ObjectId.TryParse(groupId, out ObjectId objectId))
                return BadRequest(new ErrorResult("Invalid groupId format."));

            return await CheckUserRoleAndProceed(objectId, async () =>
            {
                var memberIdsResult = await _groupRepository.GetGroupMemberIds(objectId);
                if (memberIdsResult is SuccessDataResult<List<string>> memberIdsSuccess)
                {
                    var fullnamesResult = await _userRepository.GetUserFriendsFullnames(memberIdsSuccess.Data);
                    if (fullnamesResult is SuccessDataResult<Dictionary<string, string>> success)
                        return Ok(success.Data);
                    else
                        return BadRequest(fullnamesResult);
                }
                else return BadRequest(memberIdsResult);
            });
        }

        [HttpPost("new-group")]
        public async Task<IActionResult> CreateNewGroup([FromBody] AddGroupRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorDataResult("Invalid input for a new group", ModelState.GetErrors()));

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new ErrorResult("User not authenticated."));

            var user = await _userManager.FindByIdAsync(userIdClaim.Value);
            if (user == null)
                return Unauthorized(new ErrorResult("User not found."));

            request.MemberIds.Add(user.Id);

            var newGroupResult = await _groupRepository.CreateNewGroup(user.Id, request);
            if (newGroupResult is not SuccessDataResult<ObjectId> success)
                return BadRequest(newGroupResult);

            var membershipTasks = request.MemberIds.Select(memberId =>
                _userRepository.AddUserGroupById(memberId, success.Data));
            
            var memberShipResults= await Task.WhenAll(membershipTasks);

            if (memberShipResults.All(result => result.IsSuccess))
                return Ok(new SuccessResult("Group successfully created."));
            return BadRequest(new ErrorResult("Failed to create group."));
        }

        [HttpPost("add-member")]
        public async Task<IActionResult> AddMemberToGroup([FromBody] GroupMemberRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorDataResult("Invalid input for a new member", ModelState.GetErrors()));

            if (!ObjectId.TryParse(request.GroupId, out ObjectId objectId))
                return BadRequest(new ErrorResult("Invalid groupId format."));

            return await CheckAdminAndProceed(objectId, async () =>
            {
                var addResult = await _groupRepository.AddMemberToGroup(objectId, request.MemberId);
                var membershipResult = await _userRepository.AddUserGroupById(request.MemberId, objectId);
                if (addResult.IsSuccess && membershipResult.IsSuccess)
                    return Ok(new SuccessResult("Member successfully added to the group."));
                else
                    return BadRequest(new ErrorResult("Failed to add the member to group."));
            });
        }

        [HttpPatch("update-group-details")]
        public async Task<IActionResult> UpdateGroupDetails([FromBody] UpdateGroupRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorDataResult("Invalid input for a new member", ModelState.GetErrors()));

            if (!ObjectId.TryParse(request.GroupId, out ObjectId objectId))
                return BadRequest(new ErrorResult("Invalid groupId format."));

            return await CheckAdminAndProceed(objectId, async () =>
            {
                var patchResult = await _groupRepository.UpdateGroupDetails(request);
                return patchResult.IsSuccess ? Ok(patchResult) : BadRequest(patchResult);
            });
        }

        [HttpDelete("kick-member")]
        public async Task<IActionResult> KickMemberFromGroup([FromBody] GroupMemberRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorDataResult("Invalid input for a new member", ModelState.GetErrors()));

            if (!ObjectId.TryParse(request.GroupId, out ObjectId objectId))
                return BadRequest(new ErrorResult("Invalid groupId format."));

            return await CheckAdminAndProceed(objectId, async () =>
            {
                var kickResult = await _groupRepository.KickMemberFromGroup(objectId, request.MemberId);
                if (!kickResult.IsSuccess)
                    return BadRequest(kickResult);

                var membershipResult = await _userRepository.RemoveUserGroupById(request.MemberId, objectId);
                if (!membershipResult.IsSuccess)
                    return BadRequest(new ErrorResult("Failed to kick the member from group."));

                return Ok(new SuccessResult("Member successfully kicked from the group."));
            });
        }

    }
}

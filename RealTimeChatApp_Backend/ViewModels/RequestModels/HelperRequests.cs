using System.ComponentModel.DataAnnotations;

namespace RealTimeChatApp.API.ViewModels.RequestModels
{
    public class AddFriendRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }
    }

    public class AddGroupRequest
    {
        [Required(ErrorMessage = "Group name is required")]
        [StringLength(50, ErrorMessage = "Group name cannot exceed 50 characters")]
        public string GroupName { get; set; }

        [StringLength(100, ErrorMessage = "Description cannot exceed 100 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "At least one member is required")]
        public List<string> MemberIds { get; set; }
    }
    public class GroupMemberRequest
    {
        [Required(ErrorMessage = "Group ID is required")]
        public string GroupId { get; set; }
        [Required(ErrorMessage = "Member ID is required")]
        public string MemberId { get; set; }
    }

    public class UpdateGroupRequest
    {
        [Required(ErrorMessage = "Group ID is required")]
        public string GroupId { get; set; }

        [Required(ErrorMessage = "Group name is required")]
        [StringLength(50, ErrorMessage = "Group name cannot exceed 50 characters")]
        public string GroupName { get; set; }

        [StringLength(100, ErrorMessage = "Description cannot exceed 100 characters")]
        public string Description { get; set; } = string.Empty;
    }

    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; }
    }

}

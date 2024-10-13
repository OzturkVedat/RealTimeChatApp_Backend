using System.ComponentModel.DataAnnotations;

namespace RealTimeChatApp.API.ViewModels.RequestModels
{
    public class AddFriendRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }
    }

}

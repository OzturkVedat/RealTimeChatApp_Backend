using System.ComponentModel.DataAnnotations;

namespace RealTimeChatApp.API.ViewModels.RequestModels
{
    public class AddFriendRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }
    }

    public class NewChatRequest
    {
        [Required(ErrorMessage = "Title is required")]
        public string ChatTitle { get; set; }
        [Required(ErrorMessage = "Select a friend please")]
        public string FriendId { get; set; }
    }

}

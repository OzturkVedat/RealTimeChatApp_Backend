using Microsoft.AspNetCore.SignalR;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Interface;
using RealTimeChatApp.API.ViewModels.ResultModels;

namespace RealTimeChatApp.API.Hubs
{
    public class SearchHub : Hub
    {
        private readonly IUserRepository _userRepository;

        public SearchHub(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task SearchForFriend(string fullname)
        {
            var results = await _userRepository.SearchFriendsByFullname(fullname);
            if (results is SuccessDataResult<List<SearchDetailsResponse>> success)
                await Clients.Caller.SendAsync("ReceiveSearchResults", success.Data);
        }
    }
}

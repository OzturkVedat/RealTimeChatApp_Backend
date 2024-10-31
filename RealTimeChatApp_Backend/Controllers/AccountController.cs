using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using RealTimeChatApp.API.DTOs.RequestModels;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Interface;
using RealTimeChatApp.API.Models;
using RealTimeChatApp.API.Services;
using RealTimeChatApp.API.ViewModels.RequestModels;
using RealTimeChatApp.API.ViewModels.ResultModels;
using System.Security.Claims;

namespace RealTimeChatApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<UserModel> _userManager;
        private readonly SignInManager<UserModel> _signInManager;
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        public AccountController(UserManager<UserModel> userManager, SignInManager<UserModel> signInManager,
                                 IUserRepository userRepository, IJwtService jwtService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userRepository = userRepository;
            _jwtService = jwtService;
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorDataResult("Invalid input for author registration", ModelState.GetErrors()));

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user != null)
                return Conflict(new ErrorResult("User is already registered.", ErrorType.Conflict));

            var newUser = new UserModel
            {
                FullName = request.FullName,
                Email = request.Email,
                UserName = request.Email,
                StatusMessage = "Hello there!",
                IsOnline = false,
                ProfilePictureUrl = GetRandomProfilePictureUrl()
            };
            var result = await _userManager.CreateAsync(newUser, request.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(new ErrorDataResult("Failed to create user", errors));
            }
            var registeredUser = await _userManager.FindByEmailAsync(request.Email);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, registeredUser.Id),
                new Claim(ClaimTypes.Email, registeredUser.Email)
            };
            await _userManager.AddClaimsAsync(registeredUser, claims);
            return Ok(new SuccessResult("Successfully registered the user."));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorDataResult("Invalid input for author registration", ModelState.GetErrors()));

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Unauthorized("User not found.");

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
                return Unauthorized(new ErrorResult("Invalid email or password."));

            var claims = await _userManager.GetClaimsAsync(user);

            var jwtResult = _jwtService.GenerateJwtToken(claims) as SuccessDataResult<string>;
            var refreshTokenResult = await _jwtService.GenerateRefreshToken(user.Id) as SuccessDataResult<string>;

            if (jwtResult != null && refreshTokenResult != null)
            {
                var response = new LoginResponse
                {
                    FullName = user.FullName,
                    AccessToken = jwtResult.Data,
                    RefreshToken = refreshTokenResult.Data
                };
                return Ok(new SuccessDataResult<LoginResponse>("Successfully logged in.", response));
            }
            return BadRequest(new ErrorResult("Error while trying to log in the user."));
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (ModelState.IsValid)
                return BadRequest(new ErrorDataResult("Invalid request", ModelState.GetErrors()));

            var userIdResult = await _jwtService.ValidateRefreshToken(request.RefreshToken);
            if (userIdResult is SuccessDataResult<string> userId)
            {
                if (string.IsNullOrEmpty(userId.Data))
                    return Unauthorized(new ErrorResult("Invalid refresh token."));

                var user = await _userManager.FindByIdAsync(userId.Data);
                if (user == null)
                    return Unauthorized(new ErrorResult("User not found."));

                var claims = await _userManager.GetClaimsAsync(user);
                var jwtResult = _jwtService.GenerateJwtToken(claims) as SuccessDataResult<string>;
                if (jwtResult != null)
                {
                    var response = new LoginResponse
                    {
                        FullName = user.FullName,
                        AccessToken = jwtResult.Data,
                        RefreshToken = request.RefreshToken
                    };

                    return Ok(new SuccessDataResult<LoginResponse>("Access token refreshed successfully.", response));
                }
                return BadRequest(new ErrorResult("Error while trying to refresh the token."));
            }
            else
                return BadRequest(userIdResult);    // pass the error from the service
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new ErrorResult("User not authenticated."));

            var revokeResult = await _jwtService.RevokeRefreshToken(userIdClaim.Value);
            if (revokeResult.IsSuccess)
                return Ok(new SuccessResult("Successfully logged out."));
            else
                return BadRequest(new ErrorResult("Error while logging out."));
        }

        private string GetRandomProfilePictureUrl()
        {
            string[] profilePics =
            [
             "example_pic_000.jpg",
             "example_pic_001.jpg",
             "example_pic_002.jpg",
             "example_pic_003.jpg",
             "example_pic_004.jpg",
             "example_pic_005.jpg",
             "example_pic_006.jpg",
             "example_pic_007.jpg",
             "example_pic_008.jpg",
             "example_pic_009.jpg"
            ];

            Random random = new Random();
            int index = random.Next(profilePics.Length);
            return $"https://localhost:3000/{profilePics[index]}";
        }
    }
}

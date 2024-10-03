using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RealTimeChatApp.API.DTOs.RequestModels;
using RealTimeChatApp.API.DTOs.ResultModels;
using RealTimeChatApp.API.Models;
using RealTimeChatApp.API.Services;
using System.Security.Claims;

namespace RealTimeChatApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<UserModel> _userManager;
        private readonly SignInManager<UserModel> _signInManager;
        private readonly IJwtService _jwtService;

        public AccountController(UserManager<UserModel> userManager,
                                 SignInManager<UserModel> signInManager,
                                 IJwtService jwtService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorDataResult("Invalid input for author registration", ModelState.GetErrors()));

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user != null)
                return Conflict(new ErrorResult("User is already registered."));

            var newUser = new UserModel
            {
                UserName = request.Email,  //  username is set as email for login purposes
                Email = request.Email,
                FullName = request.FullName
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
                new Claim(ClaimTypes.NameIdentifier, registeredUser.Id.ToString()),
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
            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (user == null || !result.Succeeded)
                return Unauthorized("Invalid email or password.");

            var claims = await _userManager.GetClaimsAsync(user);

            var jwtResult = _jwtService.GenerateJwtToken(claims) as SuccessDataResult<string>;
            var refreshTokenResult = await _jwtService.GenerateRefreshToken(user.Id.ToString()) as SuccessDataResult<string>;

            if (jwtResult != null && refreshTokenResult != null)
            {
                var response = new LoginResponse
                {
                    UserId = user.Id,
                    AccessToken = jwtResult.Data,
                    RefreshToken = refreshTokenResult.Data
                };
                return Ok(new SuccessDataResult<LoginResponse>("Successfully logged in.", response));
            }
            return BadRequest(new ErrorResult("Error while trying to log in the user."));

        }

    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using RealTimeChatApp.API.DTOs;
using RealTimeChatApp.API.Models;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RealTimeChatApp.API.Services
{
    public interface IJwtService
    {
        ResultModel GenerateJwtToken(IEnumerable<Claim> claims);
        Task<ResultModel> GenerateRefreshToken(string userId);
        Task<ResultModel> ValidateRefreshTokenAsync(string refreshToken);
        Task<ResultModel> RevokeRefreshToken(string userId);
    }

    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        public JwtService(IConfiguration configuration, UserManager<ApplicationUser> userManager )
        {
            _configuration = configuration;
            _userManager = userManager;
        }

        public ResultModel GenerateJwtToken(IEnumerable<Claim> claims)
        {
            SecurityKey securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("JwtSettings:SecretKey").Value));
            var signingCred = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha512Signature);
            var securityToken = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddMinutes(60),
                issuer: _configuration.GetSection("JwtSettings:Issuer").Value,
                audience: _configuration.GetSection("JwtSettings:Audience").Value,
                signingCredentials: signingCred);
            string tokenString = new JwtSecurityTokenHandler().WriteToken(securityToken);
            return new SuccessDataResult<string>("Successfully generated access token.", tokenString);
        }
        public async Task<ResultModel> GenerateRefreshToken(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return new ErrorResult("Failed to generate refresh token: User not found.");
            

            string refreshToken = Guid.NewGuid().ToString();
            DateTime expiryDate = DateTime.UtcNow.AddDays(7);

            string tokenWithExpiry = refreshToken + "." + expiryDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var result = await _userManager.SetAuthenticationTokenAsync(user, "ChatApp", "RefreshToken", tokenWithExpiry);
            if (result.Succeeded)
            {
                await _userManager.UpdateAsync(user);
                return new SuccessDataResult<string>("Successfully generated refresh token.", refreshToken);
            }
            else
            {
                var errorMessages = result.Errors.Select(error => error.Description).ToList();
                return new ErrorDataResult("Error while generating refresh token. ", errorMessages);
            }
        }
        public async Task<ResultModel> ValidateRefreshTokenAsync(string refreshToken)
        {
            var user = await GetUserByRefreshTokenAsync(refreshToken);
            if (user == null)
                return new ErrorResult("Error while validating refresh token: User not found");

            var storedTokenWithExpiry = await _userManager.GetAuthenticationTokenAsync(user, "ChatApp", "RefreshToken");
            if (storedTokenWithExpiry == null)
                return new ErrorResult("Error while validating refresh token.");
            
            string[] tokenParts = storedTokenWithExpiry.Split('.'); // Split the stored token to extract the refresh token and expiry date
            if (tokenParts.Length != 2)
                return new ErrorResult("Error while validating refresh token: Invalid token format.");


            string storedToken = tokenParts[0];
            string expiryDateString = tokenParts[1];

            if (!DateTime.TryParseExact(expiryDateString, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime expiryDate))
                return new ErrorResult("Error while validating refresh token: Invalid token time format.");


            if (storedToken == refreshToken && expiryDate > DateTime.UtcNow)
                return new SuccessResult("Successfully validated.");
            else
                return new ErrorResult("Invalid or expired token.");
        }

        private async Task<ApplicationUser> GetUserByRefreshTokenAsync(string refreshToken)
        {
            throw new NotImplementedException();
        }

        public async Task<ResultModel> RevokeRefreshToken(string userId)     // for log out
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await _userManager.RemoveAuthenticationTokenAsync(user, "ChatApp", "RefreshToken");
                return new SuccessResult("Successfully revoked the requested token.");
            }
            return new ErrorResult("Error while revoking the requested token.");
        }
    }
}

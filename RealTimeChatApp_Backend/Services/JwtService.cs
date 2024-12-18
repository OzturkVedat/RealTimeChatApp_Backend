﻿using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using RealTimeChatApp.API.DTOs.ResultModels;
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
        Task<ResultModel> ValidateRefreshToken(string refreshToken);
        Task<ResultModel> RevokeRefreshToken(string userId);
    }

    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<UserModel> _userManager;
        private readonly IMongoCollection<UserModel> _usersCollection;
        public JwtService(IConfiguration configuration, UserManager<UserModel> userManager, IMongoDatabase database)
        {
            _configuration = configuration;
            _userManager = userManager;
            _usersCollection = database.GetCollection<UserModel>("users");
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
            var newToken = new RefreshToken(); // Generate a new refresh token
            var filter = Builders<UserModel>.Filter.Eq(u => u.Id, userId);
            var update = Builders<UserModel>.Update.Set(u => u.RefreshToken, newToken);

            var result = await _usersCollection.UpdateOneAsync(filter, update);
            if (result.ModifiedCount > 0)
                return new SuccessDataResult<string>("Successfully generated a new refresh token.", newToken.Token);

            return new ErrorResult("Error while generating the refresh token.");
        }

        public async Task<ResultModel> ValidateRefreshToken(string refreshToken)
        {
            var filter = Builders<UserModel>.Filter.And(
                Builders<UserModel>.Filter.Eq(u => u.RefreshToken.Token, refreshToken),
                Builders<UserModel>.Filter.Eq(u => u.RefreshToken.IsRevoked, false),
                Builders<UserModel>.Filter.Gt(u => u.RefreshToken.ExpiryDate, DateTime.UtcNow)
            );
            var projection = Builders<UserModel>.Projection.Include(u => u.Id);
            var user = await _usersCollection.Find(filter).Project<UserModel>(projection).FirstOrDefaultAsync();

            if (user != null)
                return new SuccessDataResult<string>("Refresh token is validated for the user.", user.Id);
            
            return new ErrorResult("Invalid or expired refresh token.");
        }

        public async Task<ResultModel> RevokeRefreshToken(string userId) // For logout
        {
            if (string.IsNullOrEmpty(userId))
                return new ErrorResult("Invalid user ID.");

            var filter = Builders<UserModel>.Filter.Eq(u => u.Id, userId);
            var update = Builders<UserModel>.Update.Set(u => u.RefreshToken.IsRevoked, true);

            var result = await _usersCollection.UpdateOneAsync(filter, update);
            if (result.IsAcknowledged && result.ModifiedCount > 0)
                return new SuccessResult("Successfully revoked the requested token.");

            return new ErrorResult("Error while revoking the requested token.");
        }

    }
}

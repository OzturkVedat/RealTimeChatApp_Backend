using Amazon.Runtime.Internal;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Driver;
using RealTimeChatApp.API.Models;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RealTimeChatApp.API.Data
{
    public class Seeder
    {
        private readonly IMongoCollection<UserModel> _userCollection;
        private readonly IMongoCollection<Superadmin> _superadminCollection;
        private readonly UserManager<UserModel> _userManager;
        public Seeder(IMongoDatabase mongoDb, UserManager<UserModel> userManager)
        {
            _userCollection = mongoDb.GetCollection<UserModel>("users");
            _superadminCollection = mongoDb.GetCollection<Superadmin>("superadmins");
            _userManager = userManager;
        }

        public async Task SeedAsync()
        {
            // Seed User
            if (!await _userCollection.AsQueryable().AnyAsync())
            {
                var user = new UserModel
                {
                    Id = Guid.NewGuid().ToString(),
                    FullName = "Van Damme",
                    Email = "dammevan@email.com",
                    UserName = "dammevan@email.com",
                    StatusMessage = "Moderating..",
                    IsOnline = false,
                    ProfilePictureUrl = "https://localhost:3000/van-damme.jpg"
                };
                var password = "damme123-";
                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id),
                        new Claim(ClaimTypes.Email, user.Email)
                    };
                    await _userManager.AddClaimsAsync(user, claims);
                }
                else
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    throw new Exception($"Failed to create seeded user: {string.Join(", ", errors)}");
                }
            }

            // Seed Superadmin
            if (!await _superadminCollection.AsQueryable().AnyAsync())
            {
                var user = await _userCollection.Find(u => u.Email == "dammevan@email.com").FirstOrDefaultAsync();
                if (user != null)
                {
                    var super = new Superadmin
                    {
                        UserId = user.Id,
                        UserName = user.FullName,
                        BroadcastedIds= []
                    };
                    await _superadminCollection.InsertOneAsync(super);
                }
                else
                {
                    throw new Exception("User not found for seeding superadmins.");
                }
            }

        }
    }
}

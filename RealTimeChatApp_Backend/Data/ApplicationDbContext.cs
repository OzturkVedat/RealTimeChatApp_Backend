using MongoDB.Driver;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.MongoDB;
using MongoDbGenericRepository;
using RealTimeChatApp.API.Models;

namespace RealTimeChatApp.API.Data
{
    public class ApplicationDbContext: MongoDbContext<ApplicationUser>
    {
    }
}

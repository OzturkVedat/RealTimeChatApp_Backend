﻿using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;

namespace RealTimeChatApp.API.Models
{
    [CollectionName("roles")]
    public class RoleModel : MongoIdentityRole<string>
    {
        public string Description {  get; set; }= string.Empty;
    }

}

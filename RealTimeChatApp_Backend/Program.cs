using AspNetCore.Identity.MongoDbCore.Extensions;
using AspNetCore.Identity.MongoDbCore.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using RealTimeChatApp.API.Hubs;
using RealTimeChatApp.API.Interface;
using RealTimeChatApp.API.Models;
using RealTimeChatApp.API.Repository;
using RealTimeChatApp.API.Services;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("FixedWindowPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,            // Max 100 requests
                Window = TimeSpan.FromMinutes(15), // Per 15 minutes
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2                 // Max 2 requests in the queue
            }));
});

builder.Logging.ClearProviders();  // Clear default providers
builder.Logging.AddConsole();      // Enable console logging
builder.Logging.AddDebug();        // Enable debug logging
builder.Logging.AddEventSourceLogger();

// bson serializer, for storing as string
BsonSerializer.RegisterSerializer(new GuidSerializer(MongoDB.Bson.BsonType.String));
BsonSerializer.RegisterSerializer(new DateTimeSerializer(MongoDB.Bson.BsonType.String));
BsonSerializer.RegisterSerializer(new DateTimeOffsetSerializer(MongoDB.Bson.BsonType.String));

var mongoDbSettings = builder.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();

// modifying connection pooling
var mongoClientSettings = MongoClientSettings.FromConnectionString(mongoDbSettings.ConnectionString);
mongoClientSettings.MaxConnectionPoolSize = 200;    // max number of connections
mongoClientSettings.MinConnectionPoolSize = 5;      // min idle connections
mongoClientSettings.ConnectTimeout = TimeSpan.FromSeconds(10);


builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoClientSettings));
builder.Services.AddScoped<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(mongoDbSettings.DatabaseName);
});

var mongoDbIdentityConfig = new MongoDbIdentityConfiguration
{
    MongoDbSettings = mongoDbSettings,
    IdentityOptionsAction = options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredUniqueChars = 3;
        options.Password.RequireUppercase = false;

        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
        options.Lockout.MaxFailedAccessAttempts = 5;

        options.User.RequireUniqueEmail = true;
    }
};

builder.Services.ConfigureMongoDbIdentity<UserModel, RoleModel, string>(mongoDbIdentityConfig)
    .AddUserManager<UserManager<UserModel>>()
    .AddSignInManager<SignInManager<UserModel>>()
    .AddRoleManager<RoleManager<RoleModel>>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(auth =>
{
    auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(jwt =>
{
    jwt.RequireHttpsMetadata = true;
    jwt.SaveToken = true;
    jwt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"])),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
    jwt.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && ((path.StartsWithSegments("/hub/chat") || path.StartsWithSegments("/hub/group"))))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddScoped<IJwtService, JwtService>();

builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(option =>
{
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 1024 * 1024;        // 1MB limit
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
    options.HandshakeTimeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("https://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

var app = builder.Build();

app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowReactApp");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<ChatHub>("/hub/chat").RequireRateLimiting("FixedWindowPolicy");
app.MapHub<GroupHub>("/hub/group").RequireRateLimiting("FixedWindowPolicy");

app.MapControllers();

app.Run();
using MongoDB.Bson.IO;
using System.Net;
using Newtonsoft.Json;

namespace RealTimeChatApp.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);  
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred.");

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var response = new
                {
                    message = "An unexpected error occurred. Please try again later.",
                    error = ex.Message      // hide sensitive info if needed
                };

                await context.Response.WriteAsync(Newtonsoft.Json.JsonConvert.SerializeObject(response));
            }
        }
    }

}

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RealTimeChatApp.API.DTOs
{
    public class SuccessDataResult<T> : ResultModel
    {
        public T Data { get; set; }
        public SuccessDataResult(string message, T data)
        {
            IsSuccess = true;
            Data = data;
            Message = message;
        }
    }
    public class ErrorDataResult : ResultModel
    {
        public IEnumerable<string> Errors { get; set; }
        public ErrorDataResult(string message, IEnumerable<string> errors)
        {
            IsSuccess = false;
            Errors = errors;
            Message = message;
        }
    }
    public static class ModelStateExtensions        // for listing server-side model state errors
    {
        public static IEnumerable<string> GetErrors(this ModelStateDictionary modelState)
        {
            return modelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
        }
    }
}

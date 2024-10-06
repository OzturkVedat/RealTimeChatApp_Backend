namespace RealTimeChatApp.API.DTOs.ResultModels
{
    public class ResultModel
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }

    public class SuccessResult : ResultModel
    {
        public SuccessResult()
        {
            IsSuccess = true;
        }

        public SuccessResult(string message)
        {
            IsSuccess = true;
            Message = message;
        }
    }
    public class ErrorResult : ResultModel
    {
        public ErrorType? Type { get; set; }
        public ErrorResult()
        {
            IsSuccess = false;
            Message = "An error happened.";
            Type = ErrorType.Unknown;
        }
        public ErrorResult(string message)
        {
            IsSuccess = false;
            Message = message;
        }

        public ErrorResult(string message, ErrorType type)
        {
            IsSuccess = false;
            Message = message;
            Type = type;        // for repository layer responses
        }

    }
    public enum ErrorType
    {
        Unknown,
        NotFound,
        Unauthorized,
        Conflict,
        ServerError
    }

}

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
        public ErrorResult()
        {
            IsSuccess = false;
        }

        public ErrorResult(string message)
        {
            IsSuccess = false;
            Message = message;
        }
    }
}

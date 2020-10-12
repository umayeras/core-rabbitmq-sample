namespace WebApp.Core.Mailing
{
    public class SendEmailResult
    {
        #region ctor

        public bool IsSuccess { get; set; }

        public string Message { get; set; }

        public SendEmailResult(bool isSuccess, string message)
        {
            IsSuccess = isSuccess;
            Message = message;
        }

        #endregion

        #region factory methods

        public static SendEmailResult Success()
        {
            return new SendEmailResult(true, null);
        }

        public static SendEmailResult Success(string message)
        {
            return new SendEmailResult(true, message);
        }

        public static SendEmailResult Error(string message)
        {
            return new SendEmailResult(false, message);
        }

        #endregion
    }
}
namespace Opsi.Pocos
{
    public class Error
    {
        public Error()
        {
            // This default ctor required for JSON deserialisation.
        }

        public Error(string origin, string message) : this(origin, message, null, null)
        {
        }

        public Error(string origin, Exception exception)
        {
            InnerError = exception.InnerException != null ? new Error(origin, exception.InnerException) : null;
            Message = exception.Message;
            Origin = origin;
            StackTrace = exception.StackTrace;
        }

        public Error(string origin, string message, string stackTrace) : this(origin, message, stackTrace, null)
        {
        }

        public Error(string origin, string message, Error innerError) : this(origin, message, null, innerError)
        {
        }

        public Error(string origin, string message, string? stackTrace, Error? innerError)
        {
            InnerError = innerError;
            Message = message;
            Origin = origin;
            StackTrace = stackTrace;
        }

        public Error? InnerError { get; set; }

        public string Message { get; set; } = default!;

        public string Origin { get; set; } = default!;

        public string? StackTrace { get; set; }
    }
}
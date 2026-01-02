namespace StudentApi.Exceptions
{
    public class CustomNotFoundException : Exception
    {
        public CustomNotFoundException(string message) : base(message) { }
    }

    public class CustomValidationException : Exception
    {
        public Dictionary<string, string[]> Errors { get; }

        public CustomValidationException(Dictionary<string, string[]> errors)
            : base("Validation failed")
        {
            Errors = errors;
        }
    }

    public class CustomBusinessException : Exception
    {
        public string ErrorCode { get; }

        public CustomBusinessException(string message, string errorCode = "BUSINESS_ERROR")
            : base(message)
        {
            ErrorCode = errorCode;
        }
    }
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }

    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message) : base(message) { }
    }

    public class BadRequestException : Exception
    {
        public BadRequestException(string message) : base(message) { }
    }

    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message) : base(message) { }
    }

    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message) { }
    }
}
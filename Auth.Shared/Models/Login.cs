namespace Auth.Shared.Models
{
    public class LoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? AccessToken { get; set; }
        public object? Forgery { get; set; }
        public int StatusCode { get; set; } = 200;
    }
}
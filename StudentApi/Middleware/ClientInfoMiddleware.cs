namespace StudentApi.Middleware
{
    public class ClientInfoMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ClientInfoMiddleware> _logger;

        public ClientInfoMiddleware(RequestDelegate next, ILogger<ClientInfoMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                var ipAddress = GetClientIpAddress(context);
                var userAgent = context.Request.Headers["User-Agent"].ToString();
                var method = context.Request.Method;
                var path = context.Request.Path;

                _logger.LogInformation("Client: {IP} - {Method} {Path}", ipAddress, method, path);

                context.Items["ClientIP"] = ipAddress;
                context.Items["UserAgent"] = userAgent;

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ClientInfoMiddleware");
                await _next(context);
            }
        }

        private string GetClientIpAddress(HttpContext context)
        {
            try
            {
                var forwardedHeader = context.Request.Headers["X-Forwarded-For"].ToString();
                if (!string.IsNullOrEmpty(forwardedHeader))
                {
                    var firstIp = forwardedHeader.Split(',')[0].Trim();
                    if (!string.IsNullOrEmpty(firstIp))
                        return firstIp;
                }

                var realIpHeader = context.Request.Headers["X-Real-IP"].ToString();
                if (!string.IsNullOrEmpty(realIpHeader))
                    return realIpHeader;

                return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting client IP address");
                return "Error";
            }
        }
    }
}
using Auth.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using StudentApi.Classes;
using StudentApi.Services;
using System.Security.Claims;

namespace StudentApi.Controllers
{
    [ApiController]
    public class BaseController : ControllerBase
    {
        protected readonly IConfigService _configService;
        private readonly ILogger<BaseController> _logger;


        // Constructor with dependencies
        public BaseController(IConfigService configService, ILogger<BaseController> logger)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        //// Parameterless constructor for controllers that don't need these services
        //protected BaseController()
        //{
        //    _configService = null;
        //    _logger = null;
        //}



        // ================= Helper =================
        protected async Task LogEvent(string description, string pageName, string severity = "Info")
        {
            try
            {
                var conn = _configService.GetConnectionString("ODBCConnectionString");
                // Only try to get user ID if user is authenticated
                int? userId = null;
                try
                {
                    userId = GetCurrentUserId();
                }
                catch (UnauthorizedAccessException)
                {
                    // User is not authenticated - this is normal for login/logout events
                    userId = null;
                }
                var eEventJournal = new EEventJournal
                {
                    IP = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    PCName = Environment.MachineName,
                    UserId = userId,
                    Description = description,
                    PageName = pageName,
                    CreationDateTime = DateTime.Now,
                    Severity = severity
                };
                await CEventJournal.LogEvent(eEventJournal, conn);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging event");
            }
        }


        protected int GetCurrentUserId()
        {
            var userIdClaimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaimValue) || !int.TryParse(userIdClaimValue, out int userId))
                throw new UnauthorizedAccessException("Cannot identify the authenticated user.");
            return userId;
        }


        protected string GetClientIP()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }


    }
}
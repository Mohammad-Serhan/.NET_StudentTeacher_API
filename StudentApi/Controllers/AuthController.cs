using Auth.Shared.Classes;
using Auth.Shared.DTO;
using Auth.Shared.Services;
using AutoMapper;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace StudentApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AuthController : BaseController
    {
        private readonly IMapper _mapper;
        private readonly ITokenService _jwsService;
        private readonly IAntiforgery _antiforgery;
        private readonly IConfigService _configService;

        private readonly ILogger<AuthController> _logger;
        private readonly Auth.Shared.Services.ISimpleEncryptionService _encryptionService;
        private readonly IAuthService _authService;

        public AuthController(
            IAuthService authService,  // Injected from Auth.Shared
            IMapper mapper,
            ITokenService jwsService,
            IConfigService configService,
            IAntiforgery antiforgery,
            ISimpleEncryptionService encryptionService,
            ILogger<AuthController> logger)
            : base(configService, logger)
        {
            _authService = authService;
            _mapper = mapper;
            _jwsService = jwsService;
            _antiforgery = antiforgery;
            _logger = logger;
            _configService = configService;
            _encryptionService = encryptionService;
        }




        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginUserDTO? dto)
        {
            //try
            //{

            //if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            //{
            //    _logger.LogWarning("Login attempt with empty username or password");
            //    return BadRequest(new { message = "Username and password are required" });
            //}

            //string connStr = _configService.GetConnectionString("ODBCConnectionString");
            //if (string.IsNullOrEmpty(connStr))
            //{
            //    _logger.LogError("ODBC connection string not found");
            //    return StatusCode(500, new { message = "Database configuration error" });
            //}

            //var eUser = CUser.SelectByUsername(dto.Username, connStr);
            //if (eUser == null)
            //{
            //    _logger.LogWarning("Login failed: User {Username} not found", dto.Username);
            //    return Unauthorized(new { message = "Username or password are incorrect" });
            //}

            //if (!BCrypt.Net.BCrypt.Verify(dto.Password, eUser.Password))
            //{
            //    _logger.LogWarning("Login failed: Invalid password for user {Username}", dto.Username);
            //    return Unauthorized(new { message = "Username or password are incorrect" });
            //}

            //var userPermissions = CUser.GetUserPermissions((int)eUser.Id, connStr);
            //var accessToken = _jwsService.GenerateAccessToken(eUser, userPermissions);
            //var refreshToken = _jwsService.GenerateRefreshToken(eUser);

            //SetRefreshTokenCookie(refreshToken);

            //var forgery = _antiforgery.GetAndStoreTokens(HttpContext);


            //await LogEvent($"User {dto.Username} logged in successfully", "Auth/Login", "Info");



            if (dto == null) return BadRequest(new { message = "Invalid request data" });

            var result = await _authService.LoginAsync(dto);

            if (!result.Success)
            {
                return StatusCode(result.StatusCode, new { message = result.Message });
            }

            return Ok(new
            {
                message = result.Message,
                accessToken = result.AccessToken,
                //forgery = result.Forgery
            });
        }



        //[HttpPost]
        //[AllowAnonymous]
        //[IgnoreAntiforgeryToken]
        //public IActionResult RefreshToken()
        //{
        //    try
        //    {
        //        var refreshToken = Request.Cookies["refreshToken"];
        //        if (string.IsNullOrEmpty(refreshToken))
        //        {
        //            _logger.LogWarning("Refresh token not found in cookies");
        //            return Unauthorized(new { message = "Refresh token not found" });
        //        }

        //        var (isValid, principal) = _jwsService.ValidateToken(refreshToken);
        //        if (!isValid || principal == null)
        //        {
        //            _logger.LogWarning("Invalid refresh token");
        //            return Unauthorized(new { message = "Invalid refresh token" });
        //        }

        //        var tokenType = principal.FindFirst("token_type")?.Value;
        //        if (tokenType != "refresh")
        //        {
        //            _logger.LogWarning("Token is not a refresh token");
        //            return Unauthorized(new { message = "Not a refresh token" });
        //        }

        //        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int parsedUserId))
        //        {
        //            _logger.LogWarning("Invalid user ID in refresh token");
        //            return Unauthorized(new { message = "Invalid token claims" });
        //        }

        //        string connStr = _configService.GetConnectionString("ODBCConnectionString");
        //        if (string.IsNullOrEmpty(connStr))
        //        {
        //            _logger.LogError("ODBC connection string not found during token refresh");
        //            return StatusCode(500, new { message = "Database configuration error" });
        //        }

        //        var eUser = CUser.SelectById_Odbc(parsedUserId, connStr);
        //        if (eUser == null)
        //        {
        //            _logger.LogWarning("User {UserId} not found during token refresh", parsedUserId);
        //            return Unauthorized(new { message = "User not found" });
        //        }

        //        var userPermissions = CUser.GetUserPermissions((int)eUser.Id, connStr);
        //        var newAccessToken = _jwsService.GenerateAccessToken(eUser, userPermissions);
        //        var newRefreshToken = _jwsService.GenerateRefreshToken(eUser);

        //        _antiforgery.GetAndStoreTokens(HttpContext);
        //        SetRefreshTokenCookie(newRefreshToken);

        //        _logger.LogInformation("Token refreshed successfully for user {Username}", eUser.Username);

        //        return Ok(new
        //        {
        //            accessToken = newAccessToken,
        //            message = "Token refreshed successfully"
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error during token refresh");
        //        return StatusCode(500, new { message = "Error refreshing token" });
        //    }
        //}

        [HttpPost]

        [EnableRateLimiting("ModerateApiPolicy")]
        public async Task<IActionResult> Logout()
        {
            try
            {

                // Mirror the attributes you used when setting the cookies
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTime.UnixEpoch,             // past date
                    MaxAge = TimeSpan.Zero,                   // explicit max-age 0
                    HttpOnly = true,                          // same as when you set JWT
                    Secure = true,                            // same as when you set JWT
                    SameSite = SameSiteMode.None,              // <-- match your original value
                    Path = "/",                               // <-- match your original value
                                                              // Domain = "your.domain.com"             // <-- include if you set it originally
                };

                // Delete JWT cookie
                if (Request.Cookies.ContainsKey("jwt"))
                {
                    // You can also use Response.Cookies.Delete("jwt", cookieOptions);
                    Response.Cookies.Append("jwt", string.Empty, cookieOptions);
                }

                // Delete user_id cookie (if you ever set one). If it was accessible to JS, HttpOnly=false originally.
                if (Request.Cookies.ContainsKey("user_id"))
                {
                    var userIdDeleteOptions = new CookieOptions
                    {
                        Expires = DateTime.UnixEpoch,
                        MaxAge = TimeSpan.Zero,
                        HttpOnly = false,                     // <-- match how it was originally set
                        Secure = true,
                        SameSite = SameSiteMode.None,          // <-- match original
                        Path = "/",
                        // Domain = "your.domain.com"
                    };
                    Response.Cookies.Append("user_id", string.Empty, userIdDeleteOptions);
                }

                // Optional: clear CSRF token cookie too, if you set one (e.g., XSRF-TOKEN)
                // Response.Cookies.Append("XSRF-TOKEN", string.Empty, new CookieOptions { ...matching attributes... });

                // Prevent caching of this response
                Response.Headers["Cache-Control"] = "no-store";

                // If you maintain server-side sessions/blacklist, invalidate here as well.

                return Ok(new { message = "Logged out successfully" });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { message = "Error during logout" });
            }
        }



        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> GetCSRF()
        {
            // generate CSRf TOken
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            var token = tokens.RequestToken;


            var cookieOptionsCSRF = new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/",
                Expires = DateTime.UtcNow.AddDays(1)
            };
            Response.Cookies.Append("X-CSRF-TOKEN", token, cookieOptionsCSRF);

            return Ok(true);
        }


      
        private void SetRefreshTokenCookie(string refreshToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddHours(3),
                Path = "/"
            };
            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }
    }
}
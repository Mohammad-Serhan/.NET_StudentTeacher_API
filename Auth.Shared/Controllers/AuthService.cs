using Auth.Shared.Classes;
using Auth.Shared.DTO;
using Auth.Shared.Models;
using Auth.Shared.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using System.Net.Http;


namespace Auth.Shared.Controllers
{

    public class AuthService : IAuthService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAntiforgery _antiforgery;
        private readonly IConfigService _configService;
        private readonly ITokenService _jwsService;


        public AuthService(IHttpContextAccessor httpContextAccessor, IAntiforgery antiforgery, IConfigService configService, ITokenService jwsService)
        {
            _httpContextAccessor = httpContextAccessor;
            _antiforgery = antiforgery;
            _configService = configService;
            _jwsService = jwsService;
        }

        // ✅ Centralized, safe HttpContext access
        private HttpContext HttpContext =>
            _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("No active HttpContext");

        public async Task<LoginResult> LoginAsync(LoginUserDTO dto)
        {
            // Now the logic uses dto.Username and dto.Password
            if (string.IsNullOrEmpty(dto.Username) || string.IsNullOrEmpty(dto.Password))
            {
                return new LoginResult { Success = false, Message = "Credentials required", StatusCode = 400 };
            }

            string connStr = _configService.GetConnectionString("ODBCConnectionString");
            if (string.IsNullOrEmpty(connStr))
            {
                return new LoginResult { Success = false, Message = "Database configuration error", StatusCode = 400 };
            }

            var eUser = CUser.SelectByUsername(dto.Username, connStr);
            if (eUser == null)
            {
                return new LoginResult { Success = false, Message = "Username or password are incorrect", StatusCode = 400 };
            }
            Console.WriteLine(BCrypt.Net.BCrypt.HashPassword("admin"));

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, eUser.Password))
            {
                return new LoginResult { Success = false, Message = "Username or password are incorrect", StatusCode = 400 };
            }

            var userPermissions = CUser.GetUserPermissions((int)eUser.Id, connStr);
            var accessToken = _jwsService.GenerateAccessToken(eUser.Id.ToString(), eUser.Username.ToString(), "Auth");
            var refreshToken = _jwsService.GenerateRefreshToken(eUser.Id.ToString());

            SetRefreshTokenCookie(refreshToken);

            var forgery = _antiforgery.GetAndStoreTokens(HttpContext);

            return new LoginResult
            {
                Success = true,
                Forgery = forgery.RequestToken,
                AccessToken = accessToken,
                StatusCode = 200
            };
        }

        public async Task<bool> LogoutAsync()
        {
            return true;
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

            HttpContext.Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }
    }
}
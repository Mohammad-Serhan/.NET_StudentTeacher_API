using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Auth.Shared.Services
{
    public interface ITokenService
    {
        string GenerateAccessToken(string userId, string username, string role);
        string GenerateRefreshToken(string userId);
        ClaimsPrincipal ValidateToken(string token);
        ClaimsPrincipal ValidateAccessToken(string token);
        ClaimsPrincipal ValidateRefreshToken(string token);
        bool IsTokenExpiringSoon(string token, int minutes = 2);
    }

    public class JwtService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly SymmetricSecurityKey _securityKey;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
            _securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]));
        }

        public string GenerateAccessToken(string userId, string username, string role)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenExpiryMinutes = Convert.ToDouble(_configuration["CookieSettings:AccessToken:ExpiryMinutes"]);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
                new Claim("token_type", "access"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(tokenExpiryMinutes),
                Issuer = _configuration["JwtSettings:Issuer"],
                Audience = _configuration["JwtSettings:Audience"],
                SigningCredentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public string GenerateRefreshToken(string userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var refreshTokenExpiryDays = Convert.ToDouble(_configuration["CookieSettings:RefreshToken:ExpiryDays"]);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim("token_type", "refresh"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(refreshTokenExpiryDays),
                Issuer = _configuration["JwtSettings:Issuer"],
                Audience = _configuration["JwtSettings:Audience"],
                SigningCredentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public ClaimsPrincipal ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _securityKey,
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["JwtSettings:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["JwtSettings:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                return tokenHandler.ValidateToken(token, validationParameters, out _);
            }
            catch
            {
                return null;
            }
        }

        public ClaimsPrincipal ValidateAccessToken(string token)
        {
            var principal = ValidateToken(token);
            if (principal == null) return null;

            var tokenType = principal.FindFirst("token_type")?.Value;
            return tokenType == "access" ? principal : null;

        }

        public ClaimsPrincipal ValidateRefreshToken(string token)
        {
            var principal = ValidateToken(token);
            if (principal == null) return null;

            var tokenType = principal.FindFirst("token_type")?.Value;
            return tokenType == "refresh" ? principal : null;
        }

        public bool IsTokenExpiringSoon(string token, int minutes = 2)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var timeUntilExpiry = jwtToken.ValidTo - DateTime.UtcNow;
                return timeUntilExpiry.TotalMinutes < minutes;
            }
            catch
            {
                return true; // If can't read, treat as expired
            }
        }
    }
}
//using System.IdentityModel.Tokens.Jwt;
//using System.Security.Claims;
//using System.Text;
//using Microsoft.Extensions.Logging;
//using Microsoft.IdentityModel.Tokens;
//using StudentApi.Classes;

//namespace StudentApi.Services
//{
//    public interface IJwsService
//    {
//        string GenerateAccessToken(EUser user, List<string> permissions);
//        string GenerateRefreshToken(EUser user);
//        (bool isValid, ClaimsPrincipal? principal) ValidateToken(string token);
//        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
//    }

//    public class JwsService : IJwsService
//    {
//        private readonly IConfiguration _configuration;
//        private readonly ILogger<JwsService> _logger;
//        private readonly byte[] _secretKey;
//        private readonly string _issuer;
//        private readonly string _audience;

//        public JwsService(IConfiguration configuration, ILogger<JwsService> logger)
//        {
//            _configuration = configuration;
//            _logger = logger;

//            var jwtSettings = _configuration.GetSection("Jwt");
//            _secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? throw new ArgumentException("JWT SecretKey is required"));
//            _issuer = jwtSettings["Issuer"] ?? throw new ArgumentException("JWT Issuer is required");
//            _audience = jwtSettings["Audience"] ?? throw new ArgumentException("JWT Audience is required");
//        }


//        public string GenerateAccessToken(EUser user, List<string> permissions)
//        {
//            if (user == null) throw new ArgumentNullException(nameof(user));
//            if (permissions == null) throw new ArgumentNullException(nameof(permissions));

//            var claims = new List<Claim>
//            {
//                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
//                new(ClaimTypes.Name, user.Username),
//                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
//                new("token_type", "access")
//            };

//            claims.AddRange(permissions.Select(permission => new Claim("permissions", permission)));

//            return GenerateToken(claims, TimeSpan.FromMinutes(30));
//        }

//        public string GenerateRefreshToken(EUser user)
//        {
//            if (user == null) throw new ArgumentNullException(nameof(user));

//            var claims = new[]
//            {
//                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
//                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
//                new("token_type", "refresh")
//            };

//            return GenerateToken(claims, TimeSpan.FromHours(3));
//        }

//        public (bool isValid, ClaimsPrincipal? principal) ValidateToken(string token)
//        {
//            if (string.IsNullOrWhiteSpace(token))
//            {
//                _logger.LogWarning("Token validation failed: Token is null or empty");
//                return (false, null);
//            }

//            try
//            {
//                var tokenHandler = new JwtSecurityTokenHandler();
//                var validationParams = GetTokenValidationParameters(validateLifetime: true);

//                var principal = tokenHandler.ValidateToken(token, validationParams, out _);
//                _logger.LogDebug("Token validation successful");
//                return (true, principal);
//            }
//            catch (SecurityTokenException ex)
//            {
//                _logger.LogWarning(ex, "Token validation failed: {Error}", ex.Message);
//                return (false, null);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Unexpected error during token validation");
//                return (false, null);
//            }
//        }

//        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
//        {
//            if (string.IsNullOrWhiteSpace(token))
//            {
//                _logger.LogWarning("Get principal from expired token failed: Token is null or empty");
//                return null;
//            }

//            try
//            {
//                var tokenHandler = new JwtSecurityTokenHandler();
//                var validationParams = GetTokenValidationParameters(validateLifetime: false);

//                var principal = tokenHandler.ValidateToken(token, validationParams, out _);
//                _logger.LogDebug("Successfully extracted principal from expired token");
//                return principal;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogWarning(ex, "Failed to get principal from expired token");
//                return null;
//            }
//        }

//        private string GenerateToken(IEnumerable<Claim> claims, TimeSpan expiration)
//        {
//            var tokenDescriptor = new SecurityTokenDescriptor
//            {
//                Subject = new ClaimsIdentity(claims),
//                Expires = DateTime.UtcNow.Add(expiration),
//                Issuer = _issuer,
//                Audience = _audience,
//                SigningCredentials = new SigningCredentials(
//                    new SymmetricSecurityKey(_secretKey),
//                    SecurityAlgorithms.HmacSha512Signature)
//            };

//            var tokenHandler = new JwtSecurityTokenHandler();
//            var token = tokenHandler.CreateToken(tokenDescriptor);
//            return tokenHandler.WriteToken(token);
//        }

//        private TokenValidationParameters GetTokenValidationParameters(bool validateLifetime)
//        {
//            return new TokenValidationParameters
//            {
//                ValidateIssuerSigningKey = true,
//                IssuerSigningKey = new SymmetricSecurityKey(_secretKey),
//                ValidateIssuer = true,
//                ValidIssuer = _issuer,
//                ValidateAudience = true,
//                ValidAudience = _audience,
//                ValidateLifetime = validateLifetime,
//                ClockSkew = TimeSpan.Zero
//            };
//        }
//    }
//}
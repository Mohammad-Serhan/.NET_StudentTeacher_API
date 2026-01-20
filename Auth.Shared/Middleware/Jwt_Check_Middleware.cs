using Auth.Shared.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;


namespace Auth.Shared.MiddleWare
{

    public class Jwt_Check_Middleware
    {
        private readonly RequestDelegate _next;
        private readonly IAntiforgery _antiforgery;



        public Jwt_Check_Middleware(
            RequestDelegate next,
            IAntiforgery antiforgery
            )
        {
            _next = next;
            _antiforgery = antiforgery;
        }

        public async Task Invoke(
            HttpContext httpContext,
            ITokenService tokenService, // ✅ resolved per-request
            IConfiguration configuration
            )
        {

            // Skip authentication for public endpoints
            if (IsPublicEndpoint(configuration, httpContext.Request.Path))
            {
                await _next(httpContext);
                return;
            }


            // 2. Get tokens from cookies
            var accessToken = httpContext.Request.Cookies[$"jwt"];
            var refreshToken = httpContext.Request.Cookies[$"refreshtoken"];

            // Flag to skip CSRF validation for token refresh operations
            bool skipCsrfValidation = false;

            // 3. If JWT not exist ==> try to use refresh token
            if (string.IsNullOrEmpty(accessToken))
            {

                if (!string.IsNullOrEmpty(refreshToken))
                {

                    var refreshResult = await TryRefreshTokens(httpContext, refreshToken, tokenService);

                    if (refreshResult.Success)
                    {
                        // Set user principal
                        var newPrincipal = tokenService.ValidateAccessToken(refreshResult.NewAccessToken);
                        if (newPrincipal != null)
                        {
                            httpContext.User = newPrincipal;
                        }

                        skipCsrfValidation = true;

                        // Set new tokens in cookies
                        SetResponseCookies(httpContext,
                            refreshResult.NewAccessToken!,
                            refreshResult.NewRefreshToken!,
                            configuration);


                        generateCSRF(httpContext, configuration);


                        await _next(httpContext);
                        return;
                    }
                    else
                    {
                        httpContext.Response.StatusCode = 401;
                        await httpContext.Response.WriteAsync(refreshResult.Error ?? "Authentication failed");
                        return;
                    }
                }
                else
                {
                    // No tokens at all
                    httpContext.Response.StatusCode = 401;
                    await httpContext.Response.WriteAsync("No authentication tokens found");
                    return;
                }
            }


            try
            {
                // Validate the access token
                var principal = tokenService.ValidateAccessToken(accessToken);

                if (principal != null)
                {
                    // Token is valid
                    httpContext.User = principal;

                    if (httpContext.Request.Path.StartsWithSegments("/api/Csrf/GetCSRF"))
                    {

                        await _next(httpContext);
                        return;
                    }

                    // Validate CSRF for non-GET requests (except when skipped)
                    if (!skipCsrfValidation && ShouldValidateCsrf(httpContext))
                    {

                        if (httpContext.Request.Headers.ContainsKey($"X-CSRF-TOKEN"))
                        {
                            try
                            {
                                await _antiforgery.ValidateRequestAsync(httpContext);
                            }
                            catch (AntiforgeryValidationException ex)
                            {
                                httpContext.Response.StatusCode = 403; // Forbidden
                                await httpContext.Response.WriteAsync($"CSRF token validation failed: {ex.Message}");
                                return;
                            }
                        }
                        else
                        {
                            // does not contain CSRF, return Forbidden 
                            httpContext.Response.StatusCode = 403; // Forbidden
                            await httpContext.Response.WriteAsync("CSRF token missing");
                            return;
                        }
                    }

                    await _next(httpContext);
                    return;
                }
                else
                {
                    // Invalid token
                    httpContext.Response.StatusCode = 401;
                    await httpContext.Response.WriteAsync("Invalid access token");
                    return;
                }
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                httpContext.Response.StatusCode = 401;
                await httpContext.Response.WriteAsync("Authentication error");
                return;
            }

        }


        private bool IsPublicEndpoint(IConfiguration configuration, PathString path)
        {
            // Get the section and convert to list
            var publicEndpointsSection = configuration.GetSection("PublicEndpoints");

            // Option 1: Using GetChildren()
            var publicEndpoints = new List<string>();
            foreach (var endpoint in publicEndpointsSection.GetChildren())
            {
                publicEndpoints.Add(endpoint.Value);
            }

            foreach (var endpoint in publicEndpoints)
            {
                if (path.StartsWithSegments(endpoint))
                {
                    return true;
                }
            }

            return false;

        }




        private async Task<TokenRefreshResult> TryRefreshTokens(
            HttpContext context,
            string refreshToken,
            ITokenService tokenService)
        {
            var result = new TokenRefreshResult();

            // Validate refresh token
            var refreshPrincipal = tokenService.ValidateRefreshToken(refreshToken);

            if (refreshPrincipal == null)
            {
                result.Success = false;
                result.Error = "Invalid refresh token";
                return result;
            }

            var userId = refreshPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = refreshPrincipal.FindFirst(ClaimTypes.Name)?.Value ?? "";
            var role = refreshPrincipal.FindFirst(ClaimTypes.Role)?.Value ?? "Auth";

            if (string.IsNullOrEmpty(userId))
            {
                result.Success = false;
                result.Error = "Invalid user in refresh token";
                return result;
            }

            // Generate new tokens
            result.NewAccessToken = tokenService.GenerateAccessToken(userId, username, role);
            result.NewRefreshToken = tokenService.GenerateRefreshToken(userId);
            result.Success = true;

            return result;
        }


        private void generateCSRF(
            HttpContext context,
            IConfiguration configuration)
        {
            // generate CSRf TOken
            var tokens = _antiforgery.GetAndStoreTokens(context);
            var token = tokens.RequestToken;


            // get cookie paramteres from appsetting
            var cookieSettings = configuration.GetSection("CookieSettings:CsrfToken");

            var cookieOptionsCSRF = new CookieOptions
            {
                HttpOnly = bool.Parse(cookieSettings["HttpOnly"]),
                Secure = bool.Parse(cookieSettings["Secure"]),
                SameSite = Enum.Parse<SameSiteMode>(cookieSettings["SameSiteMode"], ignoreCase: true),
                Path = cookieSettings["Path"],
                Expires = DateTime.UtcNow.AddDays(Convert.ToDouble(cookieSettings["ExpiryDays"]))
            };


            context.Response.Cookies.Append($"X-CSRF-TOKEN", token, cookieOptionsCSRF);
        }



        private void SetResponseCookies(
            HttpContext context,
            string accessToken,
            string refreshToken,
            IConfiguration configuration)
        {
            var cookieSettings = configuration.GetSection("CookieSettings:AccessToken");


            var accessOptions = new CookieOptions
            {
                HttpOnly = bool.Parse(cookieSettings["HttpOnly"] ?? "true"),
                Secure = bool.Parse(cookieSettings["Secure"] ?? "true"),
                SameSite = Enum.Parse<SameSiteMode>(cookieSettings["SameSiteMode"], ignoreCase: true),
                Path = cookieSettings["Path"] ?? "/",
                Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(cookieSettings["ExpiryMinutes"]))
            };

            var refreshOptions = new CookieOptions
            {
                HttpOnly = bool.Parse(cookieSettings["HttpOnly"] ?? "true"),
                Secure = bool.Parse(cookieSettings["Secure"] ?? "true"),
                SameSite = Enum.Parse<SameSiteMode>(cookieSettings["SameSiteMode"], ignoreCase: true),
                Path = cookieSettings["Path"] ?? "/",
                Expires = DateTime.UtcNow.AddDays(Convert.ToDouble(configuration["CookieSettings:RefreshToken:ExpiryDays"]))
            };


            context.Response.Cookies.Append($"jwt", accessToken, accessOptions);
            context.Response.Cookies.Append($"refreshtoken", refreshToken, refreshOptions);

        }





        private bool ShouldValidateCsrf(HttpContext context)
        {
            // Validate CSRF for non-GET requests
            return !string.Equals(context.Request.Method, "GET", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(context.Request.Method, "HEAD", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(context.Request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase);
        }

    }

    public class TokenRefreshResult
    {
        public bool Success { get; set; }
        public string? NewAccessToken { get; set; }
        public string? NewRefreshToken { get; set; }
        public string? Error { get; set; }
    }

    public static class JwtAuthMiddlewareExtensions
    {
        public static IApplicationBuilder UseJwtAuthMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<Jwt_Check_Middleware>();
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using StudentApi.Attributes;
using StudentApi.Services;
using System.Security.Claims;

namespace StudentApi.Middleware
{
    public class DynamicAuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DynamicAuthorizationMiddleware> _logger;

        public DynamicAuthorizationMiddleware(RequestDelegate next, ILogger<DynamicAuthorizationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IPermissionService permissionService)
        {
            var endpoint = context.GetEndpoint();

            // Skip authorization for AllowAnonymous endpoints
            if (endpoint?.Metadata.GetMetadata<AllowAnonymousAttribute>() != null)
            {
                await _next(context);
                return;
            }

            // Check if user is authenticated
            var user = context.User;
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }

            // Check for our custom RequirePermissionAttribute
            var permissionAttribute = endpoint?.Metadata.GetMetadata<RequiredPermissionAttribute>();
            if (permissionAttribute == null)
            {
                // No permission requirement, continue to next middleware
                await _next(context);
                return;
            }

            // Get user ID from claims
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid user identity");
                return;
            }

            // Check if user has ALL required permissions
            var hasPermission = await permissionService.HasAllPermissionsAsync(permissionAttribute.Permissions);

            if (!hasPermission)
            {
                _logger.LogWarning(
                    "User {UserId} missing permissions: {Permissions} on {Path}",
                    userId,
                    string.Join(", ", permissionAttribute.Permissions),
                    context.Request.Path
                );

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Access denied. Missing required permissions.");
                return;
            }

            // User has all required permissions, continue
            await _next(context);
        }
    }
}
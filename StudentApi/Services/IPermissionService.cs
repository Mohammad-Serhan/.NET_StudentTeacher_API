using Auth.Shared.Classes;
using Auth.Shared.Services;
using Microsoft.AspNetCore.Http;
using StudentApi.Classes;
using System.Data;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace StudentApi.Services
{

    public interface IPermissionService
    {
        /// <summary>
        /// Checks if the current user has the specified permission
        /// </summary>
        Task<bool> HasPermissionAsync(string permission);

        /// <summary>
        /// Checks if a specific user has the specified permission
        /// </summary>
        Task<bool> UserHasPermissionAsync(int userId, string permission);

        /// <summary>
        /// Checks if the current user has any of the specified permissions
        /// </summary>
        Task<bool> HasAnyPermissionAsync(IEnumerable<string> permissions);

        /// <summary>
        /// Checks if the current user has all of the specified permissions
        /// </summary>
        Task<bool> HasAllPermissionsAsync(IEnumerable<string> permissions);

        /// <summary>
        /// Gets all permissions for the current user
        /// </summary>
        Task<List<string>> GetUserPermissionsAsync();

        /// <summary>
        /// Gets all permissions for a specific user by ID
        /// </summary>
        Task<List<string>> GetUserPermissionsAsync(int userId);


        /// <summary>
        /// Checks if a specific user has all of the specified permissions
        /// </summary>
        Task<bool> UserHasAllPermissionsAsync(int userId, IEnumerable<string> permissions);

        /// <summary>
        /// Checks if a specific user has any of the specified permissions
        /// </summary>
        Task<bool> UserHasAnyPermissionAsync(int userId, IEnumerable<string> permissions);

        /// <summary>
        /// Gets all roles for the current user
        /// </summary>
        Task<List<string>> GetUserRolesAsync();

        /// <summary>
        /// Checks if the current user is in the specified role
        /// </summary>
        Task<bool> IsInRoleAsync(string role);

        /// <summary>
        /// Checks if the current user is in any of the specified roles
        /// </summary>
        Task<bool> IsInAnyRoleAsync(IEnumerable<string> roles);

        /// <summary>
        /// Refreshes the permission cache for the current user
        /// </summary>
        Task RefreshPermissionsAsync();
    }


    /*
        It serves as a centralized permission/role management service for the current user
        It encapsulates:

                How to get the current user(from JWT claims via HttpContextAccessor).

                How to fetch user roles and permissions(via your database helper classes).

                How to cache them to avoid repeating expensive database queries.

                How to check specific rules easily (HasPermissionAsync, IsInRoleAsync, etc.)
    */

    public class PermissionService : IPermissionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfigService _configService;
        private readonly ILogger<PermissionService> _logger;

        // Cache user permissions to avoid repeated database calls
        private static readonly Dictionary<int, (List<string> Permissions, DateTime Expiry)> _permissionCache = new();
        private static readonly object _cacheLock = new object();
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

        public PermissionService(
            IHttpContextAccessor httpContextAccessor,
            IConfigService configService,
            ILogger<PermissionService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _configService = configService;
            _logger = logger;
        }

        public async Task<bool> HasPermissionAsync(string permission)
        {
            var permissions = await GetUserPermissionsAsync();
            return permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<bool> UserHasPermissionAsync(int userId, string permission)
        {
            var permissions = await GetUserPermissionsAsync(userId);
            return permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<bool> HasAnyPermissionAsync(IEnumerable<string> permissions)
        {
            var userPermissions = await GetUserPermissionsAsync();
            return permissions.Any(p => userPermissions.Contains(p, StringComparer.OrdinalIgnoreCase));
        }

        public async Task<bool> HasAllPermissionsAsync(IEnumerable<string> permissions)
        {
            var userPermissions = await GetUserPermissionsAsync();
            return permissions.All(p => userPermissions.Contains(p, StringComparer.OrdinalIgnoreCase));
        }

        public async Task<List<string>> GetUserPermissionsAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return new List<string>();

            return await GetUserPermissionsAsync(userId);
        }

        public async Task<List<string>> GetUserPermissionsAsync(int userId)
        {
            // Check cache first
            if (TryGetCachedPermissions(userId, out var cachedPermissions))
            {
                return cachedPermissions;
            }

            // Get from database
            var permissions = await GetPermissionsFromDatabase(userId);

            // Cache the result
            CachePermissions(userId, permissions);

            return permissions;
        }

        public async Task<List<string>> GetUserRolesAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return new List<string>();

            try
            {
                var connectionString = _configService.GetConnectionString("ODBCConnectionString");
                var roles = CUser.GetUserRoles(userId, connectionString);
                return roles.Select(r => r.RoleName ?? string.Empty)
                           .Where(r => !string.IsNullOrEmpty(r))
                           .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles for user ID {UserId}", userId);
                return new List<string>();
            }
        }

        public async Task<bool> IsInRoleAsync(string role)
        {
            var roles = await GetUserRolesAsync();
            return roles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<bool> IsInAnyRoleAsync(IEnumerable<string> roles)
        {
            var userRoles = await GetUserRolesAsync();
            return roles.Any(r => userRoles.Contains(r, StringComparer.OrdinalIgnoreCase));
        }

        public async Task RefreshPermissionsAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return;

            lock (_cacheLock)
            {
                _permissionCache.Remove(userId);
            }
        }

        #region Private Methods

        private int GetCurrentUserId()
        {
            try
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return 0;
                }
                return userId;
            }
            catch
            {
                return 0;
            }
        }

        private bool TryGetCachedPermissions(int userId, out List<string> permissions)
        {
            lock (_cacheLock)
            {
                if (_permissionCache.TryGetValue(userId, out var cacheEntry) &&
                    DateTime.UtcNow < cacheEntry.Expiry)
                {
                    permissions = cacheEntry.Permissions;
                    return true;
                }

                // Remove expired entry
                if (_permissionCache.ContainsKey(userId))
                {
                    _permissionCache.Remove(userId);
                }

                permissions = new List<string>();
                return false;
            }
        }

        private void CachePermissions(int userId, List<string> permissions)
        {
            lock (_cacheLock)
            {
                _permissionCache[userId] = (permissions, DateTime.UtcNow.Add(_cacheDuration));
            }
        }

        private async Task<List<string>> GetPermissionsFromDatabase(int userId)
        {
            try
            {
                var connectionString = _configService.GetConnectionString("ODBCConnectionString");

                // Use the new CUser method to get permissions
                var permissions = CUser.GetUserPermissions(userId, connectionString);

                return permissions ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for user ID {UserId}", userId);
                return new List<string>();
            }
        }



        public async Task<bool> UserHasAllPermissionsAsync(int userId, IEnumerable<string> permissions)
        {
            try
            {
                var connectionString = _configService.GetConnectionString("ODBCConnectionString");
                return CUser.UserHasAllPermissions(userId, permissions.ToList(), connectionString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking all permissions for user ID {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UserHasAnyPermissionAsync(int userId, IEnumerable<string> permissions)
        {
            try
            {
                var connectionString = _configService.GetConnectionString("ODBCConnectionString");
                return CUser.UserHasAnyPermission(userId, permissions.ToList(), connectionString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking any permissions for user ID {UserId}", userId);
                return false;
            }
        }

        #endregion
    }
}
using Auth.Shared.Classes;
using Auth.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentApi.Attributes;
using StudentApi.Classes;
using StudentApi.DTO;
using StudentApi.Services;
using System.Data;
using System.Diagnostics;

namespace StudentApi.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [RequiredPermission("User.Manage")]
    public class AdminController : BaseController
    {
        private readonly ILogger<AdminController> _logger;

        public AdminController(IConfigService configService, ILogger<AdminController> logger)
            : base(configService, logger)
        {
            _logger = logger;
        }

        private string GetConnectionString() => _configService.GetConnectionString("ODBCConnectionString");


        #region User Management Endpoints

        [HttpPost]
        public async Task<ActionResult<UserResponseDTO>> GetUsers([FromBody] UserFilterDTO filters)
        {
            try
            {
                string connStr = GetConnectionString();

                // Get total count with filters
                int totalCount = CUser.GetCountWithFilters(
                    connStr,
                    filters.Id,
                    filters.Username,
                    filters.Email,
                    filters.IsActive,
                    filters.CreatedDateFrom,
                    filters.CreatedDateTo,
                    filters.LastLoginFrom,
                    filters.LastLoginTo);

                // Get paginated data with filters
                var dt = CUser.SelectWithFilters(
                    connStr,
                    filters.Id,
                    filters.Username,
                    filters.Email,
                    filters.IsActive,
                    filters.CreatedDateFrom,
                    filters.CreatedDateTo,
                    filters.LastLoginFrom,
                    filters.LastLoginTo,
                    filters.Page,
                    filters.PageSize,
                    filters.SortBy ?? "Username",
                    filters.SortDescending ?? false);

                // Convert DataTable to list of UserDto
                var users = new List<UserDTO>();
                foreach (DataRow row in dt.Rows)
                {
                    users.Add(new UserDTO
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        Username = row["Username"]?.ToString() ?? string.Empty,
                        Email = row["Email"]?.ToString() ?? string.Empty,
                        IsActive = Convert.ToBoolean(row["IsActive"]),
                        CreatedDate = Convert.ToDateTime(row["CreatedDate"]),
                        LastLoginDate = row["LastLoginDate"] as DateTime?
                    });
                }

                var response = new UserResponseDTO
                {
                    Users = users,
                    TotalCount = totalCount,
                    Page = filters.Page,
                    PageSize = filters.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)filters.PageSize)
                };

                await LogEvent("Retrieved users list with filters", "Admin/GetUsers", "Info");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, new { message = "An error occurred while retrieving users" });
            }
        }



        [HttpPost]
        public async Task<ActionResult<List<int>>> GetUserRoles([FromBody] GetUserRolesRequest request)
        {
            try
            {
                var connStr = GetConnectionString();
                var roleIds = CUser.GetUserRoles(request.UserId, connStr)
                    .Select(r => r.Id ?? 0)
                    .ToList();

                await LogEvent($"Retrieved roles for user ID: {request.UserId}", "Admin/GetUserRoles", "Info");
                return Ok(roleIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user roles for user ID {UserId}", request.UserId);
                return StatusCode(500, new { message = "An error occurred while retrieving user roles" });
            }
        }



        [HttpPost]
        public async Task<IActionResult> UpdateUserRoles([FromBody] UserRoleUpdateDTO request)
        {
            try
            {
                var connStr = GetConnectionString();
                var success = CUser.UpdateUserRoles(request.UserId, request.RoleIds, connStr);

                if (success)
                {
                    await LogEvent($"Updated roles for user ID: {request.UserId}", "Admin/UpdateUserRoles", "Info");
                    return Ok(new { message = "User roles updated successfully" });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to update user roles" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user roles for user ID {UserId}", request.UserId);
                return StatusCode(500, new { message = "An error occurred while updating user roles" });
            }
        }

        #endregion

        #region Role Management Endpoints

        //[HttpPost]
        //public async Task<ActionResult<List<RoleDTO>>> GetRoles()
        //{
        //    try
        //    {
        //        var connStr = GetConnectionString();
        //        var dt = CRole.SelectAllDT_Odbc(null, connStr);

        //        var roleDtos = new List<RoleDTO>();
        //        foreach (DataRow row in dt.Rows)
        //        {
        //            roleDtos.Add(new RoleDTO
        //            {
        //                Id = Convert.ToInt32(row["Id"]),
        //                RoleName = row["RoleName"]?.ToString() ?? string.Empty,
        //                Description = row["Description"]?.ToString() ?? string.Empty
        //            });
        //        }

        //        await LogEvent("Retrieved roles list", "Admin/GetRoles", "Info");
        //        return Ok(roleDtos);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error retrieving roles");
        //        return StatusCode(500, new { message = "An error occurred while retrieving roles" });
        //    }
        //}

        [HttpPost]
        public async Task<ActionResult> CreateRole([FromBody] RoleInsertDTO request)
        {
            try
            {
                var connStr = GetConnectionString();

                // Check if role name already exists
                if (CRole.RoleNameExists(request.RoleName, connStr))
                {
                    return BadRequest(new { message = "Role name already exists" });
                }

                var role = new ERole
                {
                    RoleName = request.RoleName,
                    Description = request.Description
                };

                int affected = CRole.InsertRole(role, connStr);
                if (affected > 0)
                {
                    await LogEvent($"Created role: {request.RoleName}", "Admin/CreateRole", "Info");
                    return Ok(new { message = "Role created successfully", affected });
                }

                return StatusCode(500, new { message = "Failed to create role" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role: {RoleName}", request.RoleName);
                return StatusCode(500, new { message = "An error occurred while creating the role" });
            }
        }

        [HttpPost]
        public async Task<ActionResult> UpdateRole([FromBody] RoleUpdateDTO request)
        {
            try
            {
                var connStr = GetConnectionString();

                // Check if role name already exists (excluding current role)
                if (CRole.RoleNameExists(request.RoleName, connStr, request.Id))
                {
                    return BadRequest(new { message = $"Role name '{request.RoleName}' already exists" });
                }

                var role = new ERole
                {
                    Id = request.Id,
                    RoleName = request.RoleName,
                    Description = request.Description
                };

                int affected = CRole.UpdateRole(role, connStr);
                if (affected > 0)
                {
                    await LogEvent($"Updated role: {request.RoleName}", "Admin/UpdateRole", "Info");
                    return Ok(new { message = "Role updated successfully", affected });
                }

                return StatusCode(500, new { message = "Failed to update role" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role with ID {RoleId}", request.Id);
                return StatusCode(500, new { message = "An error occurred while updating the role" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRole([FromBody] DeleteRoleRequest request)
        {
            try
            {
                var connStr = GetConnectionString();

                // Check if role has users assigned
                var usersWithRole = CRole.GetUserRolesByRoleId(request.Id, connStr);
                if (usersWithRole.Any())
                {
                    return BadRequest(new { message = "Cannot delete role that is assigned to users" });
                }

                int affected = CRole.DeleteRole(request.Id, connStr);
                if (affected > 0)
                {
                    await LogEvent($"Deleted role with ID: {request.Id}", "Admin/DeleteRole", "Warning");
                    return Ok(new { message = "Role deleted successfully" });
                }
                else
                {
                    return NotFound(new { message = "Role not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role with ID {RoleId}", request.Id);
                return StatusCode(500, new { message = "An error occurred while deleting the role" });
            }
        }

        #endregion

        #region Permission Management Endpoints

        [HttpPost]
        public async Task<ActionResult<List<PermissionDTO>>> GetPermissions()
        {
            try
            {
                var connStr = GetConnectionString();
                var dt = CPermission.SelectAllDT(null, connStr);

                var permissionDtos = new List<PermissionDTO>();
                foreach (DataRow row in dt.Rows)
                {
                    permissionDtos.Add(new PermissionDTO
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        ActionName = row["ActionName"]?.ToString() ?? string.Empty,
                        Description = row["Description"]?.ToString() ?? string.Empty
                    });
                }

                await LogEvent("Retrieved permissions list", "Admin/GetPermissions", "Info");
                return Ok(permissionDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving permissions");
                return StatusCode(500, new { message = "An error occurred while retrieving permissions" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<List<int>>> GetRolePermissions([FromBody] GetRolePermissionsRequest request)
        {
            try
            {
                var connStr = GetConnectionString();
                var permissionDetails = CRolePermission.GetPermissionDetailsForRole(request.Id, connStr);
                await LogEvent($"Retrieved permissions for role ID: {request.Id}", "Admin/GetRolePermissions", "Info");
                return Ok(permissionDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving role permissions for role ID {RoleId}", request.Id);
                return StatusCode(500, new { message = "An error occurred while retrieving role permissions" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AssignPermissions([FromBody] AssignPermissionDTO request)
        {
            try
            {
                var connStr = GetConnectionString();
                var result = CRolePermission.AssignPermissionsToRole(request.RoleId, request.PermissionIds, connStr);

                if (result.Success)
                {
                    await LogEvent($"Updated permissions for role ID: {request.RoleId}", "Admin/AssignPermissions", "Info");
                    return Ok(new { message = "Role permissions updated successfully" });
                }
                else
                {
                    return StatusCode(500, new { message = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role permissions for role ID {RoleId}", request.RoleId);
                return StatusCode(500, new { message = "An error occurred while updating role permissions" });
            }
        }

        #endregion


        #region Event Logs

        [HttpPost]
        public ActionResult<EventLogResponseDTO> GetEventLogs([FromBody] EventLogFilterDTO filters)
        {
            try
            {
                string connStr = _configService.GetConnectionString("ODBCConnectionString");

                // Get total count with filters
                int totalCount = CEventJournal.GetCountWithFilters(
                    connStr,
                    filters.Id,
                    filters.IP,
                    filters.PCName,
                    filters.UserId,
                    filters.Description,
                    filters.PageName,
                    filters.Severity,
                    filters.DateFrom,
                    filters.DateTo);

                // Get paginated data with filters
                var dt = CEventJournal.SelectWithAdvancedFilters(
                    connStr,
                    filters.Id,
                    filters.IP,
                    filters.PCName,
                    filters.UserId,
                    filters.Description,
                    filters.PageName,
                    filters.Severity,
                    filters.DateFrom,
                    filters.DateTo,
                    filters.Page,
                    filters.PageSize,
                    filters.SortBy ?? "CreationDateTime",
                    filters.SortDescending);

                // Convert DataTable to list of EventLogDTO
                var eventLogs = new List<EventLogDTO>();
                var severitySet = new HashSet<string>();
                var pageNameSet = new HashSet<string>();

                foreach (DataRow row in dt.Rows)
                {
                    var eventLog = new EventLogDTO
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        IP = row["IP"]?.ToString() ?? string.Empty,
                        PCName = row["PCName"]?.ToString() ?? string.Empty,
                        CreationDateTime = Convert.ToDateTime(row["CreationDateTime"]),
                        UserId = row["UserId"] != DBNull.Value ? Convert.ToInt32(row["UserId"]) : 0,
                        Description = row["Description"]?.ToString() ?? string.Empty,
                        PageName = row["PageName"]?.ToString() ?? string.Empty,
                        Severity = row["Severity"]?.ToString() ?? "Info"
                    };

                    eventLogs.Add(eventLog);

                    // Add to sets for distinct values
                    if (!string.IsNullOrEmpty(eventLog.Severity))
                        severitySet.Add(eventLog.Severity);

                    if (!string.IsNullOrEmpty(eventLog.PageName))
                        pageNameSet.Add(eventLog.PageName);
                }


                var response = new EventLogResponseDTO
                {
                    EventLogs = eventLogs,
                    TotalCount = totalCount,
                    Page = filters.Page,
                    PageSize = filters.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)filters.PageSize)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving event logs");
                return StatusCode(500, new { message = "An error occurred while retrieving event logs" });
            }
        }

        #endregion
    }
}


using Auth.Shared.Classes;
using Auth.Shared.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StudentApi.Attributes;
using StudentApi.Classes;
using StudentApi.DTO;
using StudentApi.Exceptions;
using StudentApi.Services;
using System.Data;
using System.Security.Claims;

namespace StudentApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("ModerateApiPolicy")]
    public class RoleController : BaseController
    {
        private readonly IMapper _mapper;

        public RoleController(IConfigService configService, IMapper mapper, ILogger<RoleController> logger) : base(configService, logger)
        {
            _mapper = mapper;
        }

        // GET all roles
        [HttpPost]
        [RequiredPermission("Role.View")]
        public IActionResult GetRoles()
        {
            string conn = _configService.GetConnectionString("ODBCConnectionString");
            DataTable dt = CRole.GetAllRoles(conn); // DB helper
            if (dt.Rows.Count == 0)
                throw new NotFoundException("No roles found");

            string json = JsonConvert.SerializeObject(dt);
            return Content(json, "application/json");
        }

        // INSERT role
        [HttpPost]
        //[ValidateApiAntiForgeryToken]
        [RequiredPermission("Role.Insert")]
        public async Task<IActionResult> InsertRole([FromBody] RoleInsertDTO dto)
        {
            var eRole = _mapper.Map<ERole>(dto);
            string conn = _configService.GetConnectionString("ODBCConnectionString");
            var rows = CRole.InsertRole(eRole, conn);
            if (rows == 0)
                throw new BadRequestException("Failed to insert role");

            await LogEvent($"Inserted role: {eRole.RoleName}", "Rnser Role", "Info");

            return Ok(new { affected = rows, message = "Role added successfully" });
        }

        // UPDATE role
        [HttpPost]
        //[ValidateApiAntiForgeryToken]
        [RequiredPermission("Role.Update")]
        public async Task<IActionResult> UpdateRole([FromBody] RoleUpdateDTO dto)
        {
            var eRole = _mapper.Map<ERole>(dto);
            string conn = _configService.GetConnectionString("ODBCConnectionString");

            if (!CRole.RoleExists(eRole.Id, conn))
                throw new NotFoundException($"Role with ID {eRole.Id} does not exist");

            var rows = CRole.UpdateRole(eRole, conn);
            if (rows == 0)
                throw new BadRequestException("Failed to update role");

            await LogEvent($"Updated role {eRole.Id} -> {eRole.RoleName}", "Role Update", "Info");

            return Ok(new { affected = rows, message = "Role updated successfully" });
        }


        // DELETE role
        [HttpPost]
        //[ValidateApiAntiForgeryToken]
        [RequiredPermission("Role.Delete")]
        public async Task<IActionResult> DeleteRole([FromBody] RoleDeleteDTO dto)
        {
            string conn = _configService.GetConnectionString("ODBCConnectionString");
            if (!CRole.RoleExists(dto.Id, conn))
                throw new NotFoundException($"Role with ID {dto.Id} does not exist");

            var rows = CRole.DeleteRole(dto.Id, conn);
            if (rows == 0)
                throw new BadRequestException("Failed to delete role");

            await LogEvent($"Deleted role {dto.Id}", "Role Delete", "Info");

            return Ok(new { affected = rows, message = "Role deleted successfully" });
        }


        // ASSIGN permissions to role
        [HttpPost]
        //[ValidateApiAntiForgeryToken]
        [RequiredPermission("Role.ManagePermissions")]
        public async Task<IActionResult> AssignPermissions([FromBody] AssignRolePermissionsDTO dto)
        {
            string conn = _configService.GetConnectionString("ODBCConnectionString");

            if (!CRole.RoleExists(dto.RoleId, conn))
                throw new NotFoundException($"Role with ID {dto.RoleId} does not exist");

            var response = CRolePermission.AssignPermissionsToRole(dto.RoleId, dto.PermissionIds, conn);
            if (!response.Success)
                throw new BadRequestException("Failed to assign permissions");

            await LogEvent($"Assigned permissions to role {dto.RoleId}", "Assign Permissions to Role");

            return Ok(new
            {
                message = response.Message,
                RowsAffected = response.RowsAffected,
                Permission = response.PermissionNames
            });
        }

        // GET permissions of a role
        [HttpPost]
        [RequiredPermission("Role.View")]
        public IActionResult GetRolePermissions([FromBody] GetRolePermissionsDTO dto)
        {
            string conn = _configService.GetConnectionString("ODBCConnectionString");
            if (!CRole.RoleExists(dto.RoleId, conn))
                throw new NotFoundException($"Role with ID {dto.RoleId} does not exist");

            var response = CRolePermission.GetPermissionDetailsForRole((int)dto.RoleId, conn);

            return Ok(new
            {
                permissionIds = response
            });
        }


    }
}

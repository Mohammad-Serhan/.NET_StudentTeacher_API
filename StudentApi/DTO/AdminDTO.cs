using System.ComponentModel.DataAnnotations;

namespace StudentApi.DTO
{

    public class GetUserRolesRequest
    {
        public int UserId { get; set; }
    }

    public class GetRolePermissionsRequest
    {
        [Required]
        public int Id { get; set; }
    }

    // Request DTOs
    public class RolePermissionRTO { 
        public int RoleId { get; set; } 

    }
    public class AssignPermissionDTO { 
        public int RoleId { get; set; } 
        public List<int> PermissionIds { get; set; } = new();
    }
    public class UserRoleRequest { public int UserId { get; set; } }
    public class UpdateUserRolesRequest { public int UserId { get; set; } public List<int> RoleIds { get; set; } = new(); }
    public class CreateRoleRequest { public string RoleName { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; }
    public class UpdateRoleRequest { public int Id { get; set; } public string RoleName { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; }
    public class DeleteRoleRequest { public int Id { get; set; } }
}

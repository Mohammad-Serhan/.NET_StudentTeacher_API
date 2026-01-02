using System.ComponentModel.DataAnnotations;

namespace StudentApi.DTO
{
    public class RolePermissionDTO
    {
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string PermissionName { get; set; } = string.Empty;
    }


    public class AssignRolePermissionsDTO
    {
        [Required]
        public int RoleId { get; set; }
        public List<int> PermissionIds { get; set; } = new List<int>();
    }



    public class GetRolePermissionsDTO
    {
        [Required]
        public int RoleId { get; set; }
    }


    public class PermissionDetailDTO
    {
        public int PermissionId { get; set; }
        public string ActionName { get; set; } = string.Empty;
    }
}
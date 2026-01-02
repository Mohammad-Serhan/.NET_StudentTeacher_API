using System.ComponentModel.DataAnnotations;

namespace StudentApi.DTO
{
    public class RoleDTO
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }



    public class RoleInsertDTO
    {
        [Required]
        public string RoleName { get; set; } = string.Empty;
        [Required]
        public string Description { get; set; } = string.Empty;
    }



    public class RoleUpdateDTO
    {
        public int? Id { get; set; }
        public string? RoleName { get; set; } = string.Empty;

        public string? Description { get; set; } = string.Empty;
    }



    public class RoleDeleteDTO
    {
        [Required]
        public int Id { get; set; }
    }
    }
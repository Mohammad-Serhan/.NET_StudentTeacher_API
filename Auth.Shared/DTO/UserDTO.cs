using System.ComponentModel.DataAnnotations;

namespace Auth.Shared.DTO
{
    public class LoginUserDTO
    {
        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
    }

  


    public class UserFilterDTO
    {
        public int? Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedDateFrom { get; set; }
        public DateTime? CreatedDateTo { get; set; }
        public DateTime? LastLoginFrom { get; set; }
        public DateTime? LastLoginTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; }
        public bool? SortDescending { get; set; }
    }


    public class UserResponseDTO
    {
        public List<UserDTO> Users { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }


    public class UserDTO
    {
        public int? Id { get; set; }
        public string? Username { get; set; } 
        public string? Email { get; set; } 
        public bool? IsActive { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }




    public class UserRoleUpdateDTO
    {
        [Required]
        public int UserId { get; set; }
        [Required]
        public List<int> RoleIds { get; set; } = new();
    }

    public class UserWithRolesDTO : UserDTO
    {
        public List<string> Roles { get; set; } = new();
    }

    public class UpdateUserDTO
    {
        [Required]
        public int Id { get; set; }
        public string? Username { get; set; } 
        public string? Email { get; set; }
        public bool? IsActive { get; set; }
    }
}
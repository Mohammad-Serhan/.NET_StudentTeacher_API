namespace StudentApi.DTO
{
    public class PermissionDTO
    {
        public int Id { get; set; }
        public string ActionName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }


    public class AssignPermissionsResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public int RowsAffected { get; set; }
        public List<string> PermissionNames { get; set; } = new List<string>();
    }
}
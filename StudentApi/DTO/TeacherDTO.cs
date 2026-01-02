namespace StudentApi.DTO
{
    public class TeacherDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int Experience { get; set; }
    }

    public class InsertTeacherDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int Experience { get; set; }
    }

    public class UpdateTeacherDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int Experience { get; set; }
    }

    public class DeleteTeacherDTO
    {
        public int Id { get; set; }
    }

    public class TeacherFilterDTO
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Subject { get; set; }
        public int? MinExperience { get; set; }
        public int? MaxExperience { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
    }

    public class TeacherResponseDTO
    {
        public List<TeacherDTO> Teachers { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class ExportTeacherDTO
    {
        public string Format { get; set; } = "excel";
    }
}
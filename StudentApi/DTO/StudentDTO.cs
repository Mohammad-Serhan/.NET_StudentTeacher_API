using System.ComponentModel.DataAnnotations;

namespace StudentApi.DTO
{

    public class InsertStudentDTO
    {
        public string? Name { get; set; }
        public int? Age { get; set; }
        public string? Grade { get; set; }
        public int? TeacherId { get; set; }
    }

    public class StudentFilterDTO
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public int? TeacherId { get; set; }
        public string SortBy { get; set; } = "Name";
        public bool SortDescending { get; set; } = false;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class UpdateStudentDTO
    {
        [Required]
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? Age { get; set; }
        public string? Grade { get; set; } 
        public int? TeacherId { get; set; }
    }


    public class StudentResponseDTO
    {
        public List<StudentDTO> Students { get; set; } = new();
        public List<TeacherSelectDTO> Teachers { get; set; } = new List<TeacherSelectDTO>();
        public int Page { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }



    public class TeacherSelectDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class StudentDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Grade { get; set; } = string.Empty;
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
    }



    public class DeleteStudentDTO
    {
        [Required]
        public int Id { get; set; }
    }


    /// <summary>
    /// DTO for exporting students data
    /// </summary>
    public class ExportStudentDTO
    {
        public string Format { get; set; } = "pdf"; // Default to pdf
    }

    /// <summary>
    /// Response DTO for export operations
    /// </summary>
    public class ExportResponseDTO
    {
        public byte[] FileContent { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
    }


}

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
using StudentApi.Services;
using System.Data;





namespace StudentApi.Controllers
{
    // Controllers – Classes that handle API requests. 
    // - (Automapper) They call services, map DTO → Entity, and return JSON.

    /* [ApiController] attribute: Automatically handles model validation errors.
    Infers binding sources (like [FromBody] for POST requests Simplifies API development in ASP.NET Core.
     */
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize] // ← This triggers automatic token validation!
    [EnableRateLimiting("ModerateApiPolicy")]  // ✅ Apply to entire controller
    public class NewStudentController : BaseController   // base class for Web API controllers (without MVC view support).
    {
         
        private readonly IMapper _mapper; // is an AutoMapper instance, used to convert DTOs to entities (like in your StudentProfile) or vice versa.
        private readonly IExportService _exportService; // create exportable files
        private readonly ILogger<NewStudentController> _logger;


        public NewStudentController(IConfigService configService,
                                    IMapper mapper,
                                    IExportService exportService,
                                    ILogger<NewStudentController> logger) : base(configService, logger)
        {
            _mapper = mapper; // now _mapper is not null
            _exportService = exportService;
            _logger = logger;
        }


        // ✅ READ-ONLY: No CSRF protection 
        // pass a body //
        //[FromBody] → tells ASP.NET to take the JSON body from the request and convert it to GetStudentDTO




        [HttpPost]
        [RequiredPermission("Student.Insert")] // ← Allow any authenticated user that has view students permission
        //[ValidateApiAntiForgeryToken]
        public async Task<IActionResult> InsertStudent([FromBody] InsertStudentDTO dto)
        {
            try
            {
                // If execution reaches here, token is automatically validated by ASP.NET Core 
                // No manual token checking needed in controller methods! 
                // dynamic mapper
                var eStudent1 = _mapper.Map<EStudent>(dto);
                string conn = _configService.GetConnectionString("ODBCConnectionString");
                var rows = CStudent.InsertStudent(eStudent1, conn);
                if (rows > 0)
                {
                    await LogEvent($"Created student: {dto.Name}", "Student/InsertStudent", "Info");
                    return Ok(new { message = "Student created successfully", rows });
                }

                return StatusCode(500, new { message = "Failed to create student" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating student: {StudentName}", dto.Name);
                return StatusCode(500, new { message = "An error occurred while creating the student" });
            }

        }

        [HttpPost]
        [RequiredPermission("Student.Update")] // ← Allow any authenticated user that has Update students permission
        //[CustomValidateAntiForgeryToken] // <--- Use the built-in attribute
        public IActionResult UpdateStudent([FromBody] UpdateStudentDTO dto)
        {
            try
            {
                var eStudent1 = _mapper.Map<EStudent>(dto);
                string connStr = _configService.GetConnectionString("ODBCConnectionString");
                if (!CStudent.StudentExists(eStudent1.Id, connStr))
                {
                    return NotFound(new
                    {
                        message = $"Student with ID {eStudent1.Id} does not exist."
                    });
                }
                var rows = CStudent.UpdateStudent(eStudent1, connStr);
                return Ok(new { affected = rows });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


        [HttpPost]
        [RequiredPermission("Student.Delete")]
        //[ValidateApiAntiForgeryToken]
        public IActionResult DeleteStudent([FromBody] DeleteStudentDTO dto)
        {
            try
            {
                var eStudent1 = _mapper.Map<EStudent>(dto);
                string connStr = _configService.GetConnectionString("ODBCConnectionString");

                // Fix: Check if Id is null before using it
                if (eStudent1.Id == null || eStudent1.Id <= 0)
                {
                    return BadRequest(new { message = "Student ID is required and must be a positive number." });
                }

                if (!CStudent.StudentExists(eStudent1.Id.Value, connStr)) // Use .Value to get the non-nullable int
                {
                    return NotFound(new { message = $"Student with ID {eStudent1.Id} does not exist." });
                }

                var rows = CStudent.DeleteStudent(eStudent1.Id.Value, connStr); // Use .Value here too

                if (rows > 0)
                {
                    return Ok(new { message = $"Student with ID {eStudent1.Id} deleted successfully." });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to delete student." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting student with ID {StudentId}", dto?.Id);
                return StatusCode(500, new { message = "An error occurred while deleting the student." });
            }
        }


        ////// pagination and filtering 


        // ✅ READ-ONLY:
        // No CSRF protection
        [HttpPost]
        [RequiredPermission("Student.View")]
        //[IgnoreApiAntiForgeryToken]
        public ActionResult<StudentResponseDTO> GetStudents([FromBody] StudentFilterDTO filters)
        {
            try
            {
                string connStr = _configService.GetConnectionString("ODBCConnectionString");

                // Get total count with filters
                int totalCount = CStudent.GetCountWithFilters(
                    connStr,
                    filters.Id,
                    filters.Name,
                    filters.Grade,
                    filters.MinAge,
                    filters.MaxAge,
                    filters.TeacherId);

                // Get paginated data with filters
                var dt = CStudent.SelectWithAdvancedFilters(
                    connStr,
                    filters.Id,
                    filters.Name,
                    filters.Grade,
                    filters.MinAge,
                    filters.MaxAge,
                    filters.TeacherId,
                    filters.Page,
                    filters.PageSize,
                    filters.SortBy ?? "Name",
                    filters.SortDescending);

                // Convert DataTable to list of StudentDTO
                var students = new List<StudentDTO>();
                var teacherDict = new Dictionary<int, string>(); // Store teacher ID -> Name mapping

                foreach (DataRow row in dt.Rows)
                {
                    var teacherId = Convert.ToInt32(row["TeacherId"]);
                    var teacherName = row["TeacherName"]?.ToString() ?? "No Teacher";

                    students.Add(new StudentDTO
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        Name = row["Name"]?.ToString() ?? string.Empty,
                        Age = Convert.ToInt32(row["Age"]),
                        Grade = row["Grade"]?.ToString() ?? string.Empty,
                        TeacherId = teacherId,
                        TeacherName = teacherName
                    });

                    // Add to teacher dictionary if not already present
                    if (!teacherDict.ContainsKey(teacherId))
                    {
                        teacherDict[teacherId] = teacherName;
                    }
                }

                // Convert dictionary to list of TeacherDTO
                var teachers = teacherDict.Select(kvp => new TeacherSelectDTO
                {
                    Id = kvp.Key,
                    Name = kvp.Value
                }).ToList();

                var response = new StudentResponseDTO
                {
                    Students = students,
                    Teachers = teachers, // Now this is List<TeacherDTO> instead of List<string>
                    TotalCount = totalCount,
                    Page = filters.Page,
                    PageSize = filters.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)filters.PageSize)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving students");
                return StatusCode(500, new { message = "An error occurred while retrieving students" });
            }
        }



        // Export
        [HttpPost]
        [RequiredPermission("Student.View")]
        //[ValidateApiAntiForgeryToken]
        public async Task<IActionResult> ExportStudents([FromBody] ExportStudentDTO request)
        {
            try
            {
                string connStr = _configService.GetConnectionString("ODBCConnectionString");

                // Get ALL students data
                var studentsData = CStudent.SelectWithAdvancedFilters(
                    connStr,
                    page: 1,
                    pageSize: int.MaxValue // Get all records
                );

                // Check if we have data
                if (studentsData == null || studentsData.Rows.Count == 0)
                {
                    return NotFound(new { error = "No students data available for export" });
                }

                // Generate file based on requested format
                ExportResponseDTO exportResult;

                switch (request.Format.ToLower())
                {
                    case "pdf":
                        exportResult = _exportService.GeneratePdf(studentsData, "Students");
                        break;
                    case "excel":
                        exportResult = _exportService.GenerateExcel(studentsData, "Students");
                        break;
                    case "csv":
                        exportResult = _exportService.GenerateCsv(studentsData, "Students");
                        break;
                    default:
                        return BadRequest(new { error = "Unsupported export format. Supported formats: pdf, excel, csv" });
                }

                // Log the export event
                await LogEvent($"Exported students data as {request.Format.ToUpper()}", "Student/ExportStudents", "Info");

                return File(exportResult.FileContent, exportResult.ContentType, exportResult.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting students data");
                return StatusCode(500, new { error = "An error occurred while exporting students data" });
            }
        }




    }


}



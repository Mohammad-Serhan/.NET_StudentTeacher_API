using Auth.Shared.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentApi.Attributes;
using StudentApi.Classes;
using StudentApi.DTO;
using StudentApi.Services;
using System.Data;

namespace StudentApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("ModerateApiPolicy")]
    public class TeacherController : BaseController
    {
        private readonly IMapper _mapper;
        private readonly IExportService _exportService;
        private readonly ILogger<TeacherController> _logger;

        public TeacherController(
            IConfigService configService,
            IMapper mapper,
            IExportService exportService,
            ILogger<TeacherController> logger) : base(configService, logger)
        {
            _mapper = mapper;
            _exportService = exportService;
            _logger = logger;
        }

        [HttpPost]
        [RequiredPermission("Teacher.Insert")]
        //[ValidateApiAntiForgeryToken]
        public async Task<IActionResult> InsertTeacher([FromBody] InsertTeacherDTO dto)
        {
            try
            {
                var eTeacher = _mapper.Map<ETeacher>(dto);
                string conn = _configService.GetConnectionString("ODBCConnectionString");
                var rows = CTeacher.InsertTeacher(eTeacher, conn);

                if (rows > 0)
                {
                    await LogEvent($"Created teacher: {dto.Name}", "Teacher/InsertTeacher", "Info");
                    return Ok(new { message = "Teacher created successfully", rows });
                }

                return StatusCode(500, new { message = "Failed to create teacher" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating teacher: {TeacherName}", dto.Name);
                return StatusCode(500, new { message = "An error occurred while creating the teacher" });
            }
        }

        [HttpPost]
        [RequiredPermission("Teacher.Update")]
        //[ValidateApiAntiForgeryToken]
        public IActionResult UpdateTeacher([FromBody] UpdateTeacherDTO dto)
        {
            try
            {
                var eTeacher = _mapper.Map<ETeacher>(dto);
                string connStr = _configService.GetConnectionString("ODBCConnectionString");

                if (!CTeacher.TeacherExists(eTeacher.Id, connStr))
                {
                    return NotFound(new { message = $"Teacher with ID {eTeacher.Id} does not exist." });
                }

                var rows = CTeacher.UpdateTeacher(eTeacher, connStr);
                return Ok(new { affected = rows });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost]
        [RequiredPermission("Teacher.Delete")]
        //[ValidateApiAntiForgeryToken]
        public IActionResult DeleteTeacher([FromBody] DeleteTeacherDTO dto)
        {
            try
            {
                string connStr = _configService.GetConnectionString("ODBCConnectionString");

                if (!CTeacher.TeacherExists(dto.Id, connStr))
                {
                    return NotFound(new { message = $"Teacher with ID {dto.Id} does not exist." });
                }

                var rows = CTeacher.DeleteTeacher(dto.Id, connStr);
                return Ok(new { affected = rows });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost]
        [RequiredPermission("Teacher.View")]
        //[IgnoreApiAntiForgeryToken]
        public async Task<ActionResult<TeacherResponseDTO>> GetTeachers([FromBody] TeacherFilterDTO filters)
        {
            try
            {
                string connStr = _configService.GetConnectionString("ODBCConnectionString");

                // Get total count with filters
                int totalCount = CTeacher.GetCountWithFilters(
                    connStr,
                    filters.Id,
                    filters.Name,
                    filters.Subject,
                    filters.MinExperience,
                    filters.MaxExperience);

                // Get paginated data with filters
                var dt = CTeacher.SelectWithAdvancedFilters(
                    connStr,
                    filters.Id,
                    filters.Name,
                    filters.Subject,
                    filters.MinExperience,
                    filters.MaxExperience,
                    filters.Page,
                    filters.PageSize,
                    filters.SortBy ?? "Name",
                    filters.SortDescending);

                // Convert DataTable to list of TeacherDTO
                var teachers = new List<TeacherDTO>();
                foreach (DataRow row in dt.Rows)
                {
                    teachers.Add(new TeacherDTO
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        Name = row["Name"]?.ToString() ?? string.Empty,
                        Subject = row["Subject"]?.ToString() ?? string.Empty,
                        Experience = Convert.ToInt32(row["Experience"])
                    });
                }

                var response = new TeacherResponseDTO
                {
                    Teachers = teachers,
                    TotalCount = totalCount,
                    Page = filters.Page,
                    PageSize = filters.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)filters.PageSize)
                };

                await LogEvent("Retrieved teachers list with filters", "Teacher/GetTeachers", "Info");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving teachers");
                return StatusCode(500, new { message = "An error occurred while retrieving teachers" });
            }
        }

        [HttpPost]
        [RequiredPermission("Teacher.View")]
        //[ValidateApiAntiForgeryToken]
        public async Task<IActionResult> ExportTeachers([FromBody] ExportTeacherDTO request)
        {
            try
            {
                string connStr = _configService.GetConnectionString("ODBCConnectionString");

                // Get ALL teachers data
                var teachersData = CTeacher.SelectWithAdvancedFilters(
                    connStr,
                    page: 1,
                    pageSize: int.MaxValue
                );

                if (teachersData == null || teachersData.Rows.Count == 0)
                {
                    return NotFound(new { error = "No teachers data available for export" });
                }

                ExportResponseDTO exportResult;

                switch (request.Format.ToLower())
                {
                    case "pdf":
                        exportResult = _exportService.GeneratePdf(teachersData, "Teachers");
                        break;
                    case "excel":
                        exportResult = _exportService.GenerateExcel(teachersData, "Teachers");
                        break;
                    case "csv":
                        exportResult = _exportService.GenerateCsv(teachersData, "Teachers");
                        break;
                    default:
                        return BadRequest(new { error = "Unsupported export format" });
                }

                await LogEvent($"Exported teachers data as {request.Format.ToUpper()}", "Teacher/ExportTeachers", "Info");
                return File(exportResult.FileContent, exportResult.ContentType, exportResult.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting teachers data");
                return StatusCode(500, new { error = "An error occurred while exporting teachers data" });
            }
        }
    }
}
namespace StudentApi.DTO
{
    public class EventLogFilterDTO
    {
        public int? Id { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; }
        public string? IP { get; set; }
        public string? PCName { get; set; }
        public int? UserId { get; set; }
        public string? Description { get; set; }
        public string? PageName { get; set; }
        public string? Severity { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }

    public class EventLogResponseDTO
    {
        public List<EventLogDTO> EventLogs { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class EventLogDTO
    {
        public int Id { get; set; }
        public string IP { get; set; } = string.Empty;
        public string PCName { get; set; } = string.Empty;
        public DateTime CreationDateTime { get; set; }
        public int UserId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string PageName { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
    }
}
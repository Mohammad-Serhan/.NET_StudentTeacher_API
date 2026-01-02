using System.Data;
using System.Data.Odbc;
using System.Data.SqlTypes;
using System.Text;

namespace StudentApi.Classes
{
    public class EEventJournal
    {
        public int? Id { get; set; }
        public string? IP { get; set; }
        public string? PCName { get; set; }
        public DateTime CreationDateTime { get; set; } = DateTime.UtcNow;
        public int? UserId { get; set; }
        public string? Description { get; set; }
        public string? PageName { get; set; } 
        public string? Severity { get; set; } 
    }

    public class CEventJournal
    {



        public static DataTable SelectWithAdvancedFilters(
        string connectionString,
        int? id = null,
        string? ip = null,
        string? pcName = null,
        int? userId = null,
        string? description = null,
        string? pageName = null,
        string? severity = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 10,
        string? sortBy = "CreationDateTime",
        bool? sortDescending = true) // Default sort by most recent
        {
            using (var cn = new OdbcConnection(connectionString))
            {
                cn.Open();
                var sb = new StringBuilder(@"
                SELECT j.*, u.Username as UserName 
                FROM EventJournal j 
                LEFT JOIN Users u ON j.UserId = u.Id 
                WHERE 1=1");

                var parameters = new List<OdbcParameter>();
                var conditions = new List<string>();

                // Apply filters
                if (id.HasValue)
                {
                    conditions.Add("j.Id = ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = id.Value });
                }
                if (!string.IsNullOrWhiteSpace(ip))
                {
                    conditions.Add("j.IP LIKE ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = $"%{ip}%" });
                }
                if (!string.IsNullOrWhiteSpace(pcName))
                {
                    conditions.Add("j.PCName LIKE ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = $"%{pcName}%" });
                }
                if (userId.HasValue)
                {
                    conditions.Add("j.UserId = ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = userId.Value });
                }
                if (!string.IsNullOrWhiteSpace(description))
                {
                    conditions.Add("j.Description LIKE ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = $"%{description}%" });
                }
                if (!string.IsNullOrWhiteSpace(pageName))
                {
                    conditions.Add("j.PageName LIKE ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = $"%{pageName}%" });
                }
                if (!string.IsNullOrWhiteSpace(severity))
                {
                    conditions.Add("j.Severity = ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = severity });
                }
                if (startDate.HasValue)
                {
                    conditions.Add("j.CreationDateTime >= ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.DateTime, Value = startDate.Value });
                }
                if (endDate.HasValue)
                {
                    conditions.Add("j.CreationDateTime <= ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.DateTime, Value = endDate.Value });
                }

                if (conditions.Count > 0) sb.Append(" AND ").Append(string.Join(" AND ", conditions));

                // Apply sorting
                var validSortColumns = new[] { "Id", "IP", "PCName", "CreationDateTime", "UserId", "Description", "PageName", "Severity" };
                var sortColumn = validSortColumns.Contains(sortBy) ? sortBy : "CreationDateTime";
                var sortDirection = (bool)sortDescending ? "DESC" : "ASC";
                sb.Append($" ORDER BY {sortColumn} {sortDirection}");

                // Apply pagination
                sb.Append($" OFFSET {(page - 1) * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY");

                var cmd = cn.CreateCommand();
                cmd.CommandText = sb.ToString();
                foreach (var p in parameters) cmd.Parameters.Add(p);

                var dt = new DataTable();
                using (var da = new OdbcDataAdapter(cmd)) da.Fill(dt);

                return dt;
            }
        }

        public static int GetCountWithFilters(
            string connectionString,
            int? id = null,
            string? ip = null,
            string? pcName = null,
            int? userId = null,
            string? description = null,
            string? pageName = null,
            string? severity = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            using (var cn = new OdbcConnection(connectionString))
            {
                cn.Open();
                var sb = new StringBuilder("SELECT COUNT(*) FROM EventJournal j WHERE 1=1");
                var parameters = new List<OdbcParameter>();
                var conditions = new List<string>();

                // Apply filters (same as SelectWithAdvancedFilters)
                if (id.HasValue)
                {
                    conditions.Add("j.Id = ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = id.Value });
                }
                if (!string.IsNullOrWhiteSpace(ip))
                {
                    conditions.Add("j.IP LIKE ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = $"%{ip}%" });
                }
                if (!string.IsNullOrWhiteSpace(pcName))
                {
                    conditions.Add("j.PCName LIKE ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = $"%{pcName}%" });
                }
                if (userId.HasValue)
                {
                    conditions.Add("j.UserId = ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = userId.Value });
                }
                if (!string.IsNullOrWhiteSpace(description))
                {
                    conditions.Add("j.Description LIKE ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = $"%{description}%" });
                }
                if (!string.IsNullOrWhiteSpace(pageName))
                {
                    conditions.Add("j.PageName LIKE ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = $"%{pageName}%" });
                }
                if (!string.IsNullOrWhiteSpace(severity))
                {
                    conditions.Add("j.Severity = ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = severity });
                }
                if (startDate.HasValue)
                {
                    conditions.Add("j.CreationDateTime >= ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.DateTime, Value = startDate.Value });
                }
                if (endDate.HasValue)
                {
                    conditions.Add("j.CreationDateTime <= ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.DateTime, Value = endDate.Value });
                }

                if (conditions.Count > 0) sb.Append(" AND ").Append(string.Join(" AND ", conditions));

                var cmd = cn.CreateCommand();
                cmd.CommandText = sb.ToString();
                foreach (var p in parameters) cmd.Parameters.Add(p);

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }



        public static async Task<int> LogEvent(EEventJournal journal, string odbcConnectionString)
        {
            using (var cn = new OdbcConnection(odbcConnectionString))
            {
                await cn.OpenAsync();
                using (var cmd = cn.CreateCommand())
                {
                    


                    cmd.CommandText = "INSERT INTO EventJournal (IP, PCName, CreationDateTime, UserId, Description, PageName, Severity) VALUES (?, ?, GETDATE(), ?, ?, ?, ?)";
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = journal.IP });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = journal.PCName  });
                    //cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.DateTime, Value = journal.CreationDateTime });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = journal.UserId });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = journal.Description  });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = journal.PageName });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = journal.Severity  });

                    return await cmd.ExecuteNonQueryAsync();
                }
            }
        }
        


    }
}
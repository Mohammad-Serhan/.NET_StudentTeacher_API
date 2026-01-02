using System.Data;
using System.Data.Odbc;
using System.Text;

namespace StudentApi.Classes
{
    public class EStudent
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public int? Age { get; set; }
        public string? Grade { get; set; }
        public int? TeacherId { get; set; }
    }

    public class CStudent
    {
        #region Select Methods
        public static DataTable SelectAllDT(EStudent eStudent, string odbcConnectionString)
        {
            using (var cn = new OdbcConnection(odbcConnectionString))
            {
                cn.Open();
                return SelectAllDT(eStudent, cn);
            }
        }

        public static DataTable SelectAllDT(EStudent eStudent, OdbcConnection odbcConnection, OdbcTransaction tx = null)
        {
            var sb = new StringBuilder();
            var dt = new DataTable();
            var cmd = odbcConnection.CreateCommand();
            cmd.Transaction = tx;

            sb.Append("SELECT s.*, t.Name as TeacherName FROM Students s LEFT JOIN Teachers t ON s.TeacherId = t.Id WHERE 1=1");
            var ands = new List<string>();
            var parameters = new List<OdbcParameter>();

            if (eStudent != null)
            {
                AddEqIfHasValue("s.Id", eStudent.Id, ands, parameters);
                AddLikeIfHasValue("s.Name", eStudent.Name, ands, parameters);
                AddEqIfHasValue("s.Age", eStudent.Age, ands, parameters);
                AddEqIfHasValue("s.Grade", eStudent.Grade, ands, parameters);
                AddEqIfHasValue("s.TeacherId", eStudent.TeacherId, ands, parameters);
            }

            foreach (var a in ands) sb.Append(" AND ").Append(a);

            cmd.CommandText = sb.ToString();
            foreach (var p in parameters) cmd.Parameters.Add(p);

            using (var da = new OdbcDataAdapter(cmd))
            {
                da.Fill(dt);
            }
            return dt;
        }

        public static DataTable SelectWithAdvancedFilters(
            string connectionString,
            int? id = null,
            string? name = null,
            string? grade = null,
            int? minAge = null,
            int? maxAge = null,
            int? teacherId = null,
            int page = 1,
            int pageSize = 10,
            string? sortBy = "Name",
            bool? sortDescending = false)
        {
            using (var cn = new OdbcConnection(connectionString))
            {
                cn.Open();
                var sb = new StringBuilder("SELECT s.*, t.Name as TeacherName FROM Students s LEFT JOIN Teachers t ON s.TeacherId = t.Id WHERE 1=1");
                var parameters = new List<OdbcParameter>();
                var conditions = new List<string>();

                // Apply filters
                if (id.HasValue)
                {
                    conditions.Add("s.Id = ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = id.Value });
                }
                if (!string.IsNullOrWhiteSpace(name))
                {
                    conditions.Add("s.Name LIKE ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = $"%{name}%" });
                }
                if (!string.IsNullOrWhiteSpace(grade))
                {
                    conditions.Add("s.Grade = ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = grade });
                }
                if (minAge.HasValue)
                {
                    conditions.Add("s.Age >= ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = minAge.Value });
                }
                if (maxAge.HasValue)
                {
                    conditions.Add("s.Age <= ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = maxAge.Value });
                }
                if (teacherId.HasValue)
                {
                    conditions.Add("s.TeacherId = ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = teacherId.Value });
                }

                if (conditions.Count > 0) sb.Append(" AND ").Append(string.Join(" AND ", conditions));

                // Apply sorting
                var validSortColumns = new[] { "Id", "Name", "Age", "Grade", "TeacherId", "TeacherName" };
                var sortColumn = validSortColumns.Contains(sortBy) ? sortBy : "Name";
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
            string? name = null,
            string? grade = null,
            int? minAge = null,
            int? maxAge = null,
            int? teacherId = null)
        {
            using (var cn = new OdbcConnection(connectionString))
            {
                cn.Open();
                var sb = new StringBuilder("SELECT COUNT(*) FROM Students s WHERE 1=1");
                var parameters = new List<OdbcParameter>();
                var conditions = new List<string>();

                // Apply filters (same as SelectWithFilters)
                if (id.HasValue)
                {
                    conditions.Add("s.Id = ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = id.Value });
                }
                if (!string.IsNullOrWhiteSpace(name))
                {
                    conditions.Add("s.Name LIKE ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = $"%{name}%" });
                }
                if (!string.IsNullOrWhiteSpace(grade))
                {
                    conditions.Add("s.Grade = ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = grade });
                }
                if (minAge.HasValue)
                {
                    conditions.Add("s.Age >= ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = minAge.Value });
                }
                if (maxAge.HasValue)
                {
                    conditions.Add("s.Age <= ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = maxAge.Value });
                }
                if (teacherId.HasValue)
                {
                    conditions.Add("s.TeacherId = ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = teacherId.Value });
                }

                if (conditions.Count > 0) sb.Append(" AND ").Append(string.Join(" AND ", conditions));

                var cmd = cn.CreateCommand();
                cmd.CommandText = sb.ToString();
                foreach (var p in parameters) cmd.Parameters.Add(p);

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static EStudent SelectById(int studentId, string odbcConnectionString)
        {
            var filter = new EStudent { Id = studentId };
            var dt = SelectAllDT(filter, odbcConnectionString);
            return DataTableToStudent(dt);
        }
        #endregion

        #region CRUD Operations
        

        public static int InsertStudent(EStudent student, string odbcConnectionString)
        {
            using (var cn = new OdbcConnection(odbcConnectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {

//Security Importance - SQL Injection Prevention

//BAD Approach (Vulnerable):

// ❌ DANGEROUS - SQL injection possible
//string query = $"SELECT * FROM OwnNWStatus WHERE NWCode = '{userInput}'";

//GOOD Approach (What This Function Does):
//csharp

// ✅ SAFE - Uses parameters
//StringBuilder query = new StringBuilder("SELECT * FROM OwnNWStatus WHERE NWCode = ?");
//parameters.Add(new OdbcParameter("@NWCode", userInput));
                    cmd.CommandText = "INSERT INTO Students (Name, Age, Grade, TeacherId) VALUES (?, ?, ?, ?)";
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = (object)student.Name ?? DBNull.Value });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = student.Age ?? (object)DBNull.Value });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = (object)student.Grade ?? DBNull.Value });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = student.TeacherId ?? (object)DBNull.Value });
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        

        public static int UpdateStudent(EStudent student, string odbcConnectionString)
        {
            using (var cn = new OdbcConnection(odbcConnectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE Students SET Name = ?, Age = ?, Grade = ?, TeacherId = ? WHERE Id = ?";
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = (object)student.Name ?? DBNull.Value });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = student.Age ?? (object)DBNull.Value });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = (object)student.Grade ?? DBNull.Value });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = student.TeacherId ?? (object)DBNull.Value });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = student.Id ?? (object)DBNull.Value });
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        

        public static int DeleteStudent(int id, string odbcConnectionString)
        {
            using (var cn = new OdbcConnection(odbcConnectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Students WHERE Id = ?";
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = id });
                    return cmd.ExecuteNonQuery();
                }
            }
        }
        #endregion


        #region Utility Methods
        public static bool StudentExists(int? id, string odbcConnectionString)
        {
            var studentByIdOnly = new EStudent { Id = id };
            var dt = SelectAllDT(studentByIdOnly, odbcConnectionString);
            return dt.Rows.Count > 0;
        }

        public static bool StudentNameExists(string name, string odbcConnectionString, int? excludeId = null)
        {
            var filter = new EStudent { Name = name };
            var dt = SelectAllDT(filter, odbcConnectionString);

            if (excludeId.HasValue)
            {
                return dt.AsEnumerable()
                    .Any(row => row.Field<int>("Id") != excludeId.Value);
            }

            return dt.Rows.Count > 0;
        }

        
        #endregion

        #region Private Helper Methods
        

        private static EStudent DataTableToStudent(DataTable dt)
        {
            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new EStudent
            {
                Id = row.Field<int?>("Id"),
                Name = row.Field<string>("Name"),
                Age = row.Field<int?>("Age"),
                Grade = row.Field<string>("Grade"),
                TeacherId = row.Field<int?>("TeacherId")
            };
        }

        private static void AddEqIfHasValue(string column, int? value, List<string> ands, List<OdbcParameter> parameters)
        {
            if (value.HasValue)
            {
                ands.Add($"{column} = ?");
                parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = value.Value });
            }
        }

        private static void AddEqIfHasValue(string column, string value, List<string> ands, List<OdbcParameter> parameters)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                ands.Add($"{column} = ?");
                parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = value });
            }
        }

        private static void AddLikeIfHasValue(string column, string value, List<string> ands, List<OdbcParameter> parameters)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                ands.Add($"{column} LIKE ?");
                parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = $"%{value}%" });
            }
        }
        #endregion
    }
}
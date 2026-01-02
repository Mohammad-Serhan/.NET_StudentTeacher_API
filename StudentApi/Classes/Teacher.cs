using System.Data;
using System.Data.Odbc;
using System.Text;

namespace StudentApi.Classes
{
    public class ETeacher
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Subject { get; set; }
        public int? Experience { get; set; }
    }

    public class CTeacher
    {
        #region Select Methods
        public static DataTable SelectAllDT(ETeacher eTeacher, string odbcConnectionString)
        {
            using (var cn = new OdbcConnection(odbcConnectionString))
            {
                cn.Open();
                return SelectAllDT(eTeacher, cn);
            }
        }

        public static DataTable SelectAllDT(ETeacher eTeacher, OdbcConnection odbcConnection, OdbcTransaction tx = null)
        {
            var sb = new StringBuilder();
            var dt = new DataTable();
            var cmd = odbcConnection.CreateCommand();
            cmd.Transaction = tx;

            sb.Append("SELECT * FROM Teachers WHERE 1=1");
            var ands = new List<string>();
            var parameters = new List<OdbcParameter>();

            if (eTeacher != null)
            {
                AddEqIfHasValue("Id", eTeacher.Id, ands, parameters);
                AddLikeIfHasValue("Name", eTeacher.Name, ands, parameters);
                AddEqIfHasValue("Subject", eTeacher.Subject, ands, parameters);
                AddEqIfHasValue("Experience", eTeacher.Experience, ands, parameters);
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
            string? subject = null,
            int? minExperience = null,
            int? maxExperience = null,
            int page = 1,
            int pageSize = 10,
            string? sortBy = "Name",
            bool? sortDescending = false)
        {
            using (var cn = new OdbcConnection(connectionString))
            {
                cn.Open();
                var sb = new StringBuilder("SELECT * FROM Teachers WHERE 1=1");
                var parameters = new List<OdbcParameter>();
                var conditions = new List<string>();

                // Apply filters
                if (id.HasValue)
                {
                    conditions.Add("Id = ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = id.Value });
                }
                if (!string.IsNullOrWhiteSpace(name))
                {
                    conditions.Add("Name LIKE ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = $"%{name}%" });
                }
                if (!string.IsNullOrWhiteSpace(subject))
                {
                    conditions.Add("Subject LIKE ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = $"%{subject}%" });
                }
                if (minExperience.HasValue)
                {
                    conditions.Add("Experience >= ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = minExperience.Value });
                }
                if (maxExperience.HasValue)
                {
                    conditions.Add("Experience <= ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = maxExperience.Value });
                }

                if (conditions.Count > 0) sb.Append(" AND ").Append(string.Join(" AND ", conditions));

                // Apply sorting
                var validSortColumns = new[] { "Id", "Name", "Subject", "Experience" };
                var sortColumn = validSortColumns.Contains(sortBy) ? sortBy : "Name";
                var sortDirection = sortDescending == true ? "DESC" : "ASC";
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
            string? subject = null,
            int? minExperience = null,
            int? maxExperience = null)
        {
            using (var cn = new OdbcConnection(connectionString))
            {
                cn.Open();
                var sb = new StringBuilder("SELECT COUNT(*) FROM Teachers WHERE 1=1");
                var parameters = new List<OdbcParameter>();
                var conditions = new List<string>();

                // Apply filters (same as SelectWithFilters)
                if (id.HasValue)
                {
                    conditions.Add("Id = ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = id.Value });
                }
                if (!string.IsNullOrWhiteSpace(name))
                {
                    conditions.Add("Name LIKE ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = $"%{name}%" });
                }
                if (!string.IsNullOrWhiteSpace(subject))
                {
                    conditions.Add("Subject LIKE ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = $"%{subject}%" });
                }
                if (minExperience.HasValue)
                {
                    conditions.Add("Experience >= ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = minExperience.Value });
                }
                if (maxExperience.HasValue)
                {
                    conditions.Add("Experience <= ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = maxExperience.Value });
                }

                if (conditions.Count > 0) sb.Append(" AND ").Append(string.Join(" AND ", conditions));

                var cmd = cn.CreateCommand();
                cmd.CommandText = sb.ToString();
                foreach (var p in parameters) cmd.Parameters.Add(p);

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static ETeacher SelectById(int teacherId, string odbcConnectionString)
        {
            var filter = new ETeacher { Id = teacherId };
            var dt = SelectAllDT(filter, odbcConnectionString);
            return DataTableToTeacher(dt);
        }

        /// <summary>
        /// Gets all teachers from the database
        /// </summary>
        public static DataTable GetAllTeachers(string connectionString)
        {
            return SelectAllDT(null, connectionString);
        }
        #endregion

        #region CRUD Operations
        public static int InsertTeacher(ETeacher teacher, string odbcConnectionString)
        {
            using (var cn = new OdbcConnection(odbcConnectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO Teachers (Name, Subject, Experience) VALUES (?, ?, ?)";
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = (object)teacher.Name ?? DBNull.Value });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = (object)teacher.Subject ?? DBNull.Value });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = teacher.Experience ?? (object)DBNull.Value });
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        public static int UpdateTeacher(ETeacher teacher, string odbcConnectionString)
        {
            using (var cn = new OdbcConnection(odbcConnectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE Teachers SET Name = ?, Subject = ?, Experience = ? WHERE Id = ?";
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = (object)teacher.Name ?? DBNull.Value });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = (object)teacher.Subject ?? DBNull.Value });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = teacher.Experience ?? (object)DBNull.Value });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = teacher.Id ?? (object)DBNull.Value });
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        public static int DeleteTeacher(int id, string odbcConnectionString)
        {
            using (var cn = new OdbcConnection(odbcConnectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Teachers WHERE Id = ?";
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = id });
                    return cmd.ExecuteNonQuery();
                }
            }
        }
        #endregion

        #region Utility Methods
        public static bool TeacherExists(int? id, string odbcConnectionString)
        {
            var teacherByIdOnly = new ETeacher { Id = id };
            var dt = SelectAllDT(teacherByIdOnly, odbcConnectionString);
            return dt.Rows.Count > 0;
        }

        public static bool TeacherNameExists(string name, string odbcConnectionString, int? excludeId = null)
        {
            var filter = new ETeacher { Name = name };
            var dt = SelectAllDT(filter, odbcConnectionString);

            if (excludeId.HasValue)
            {
                return dt.AsEnumerable()
                    .Any(row => row.Field<int>("Id") != excludeId.Value);
            }

            return dt.Rows.Count > 0;
        }

        /// <summary>
        /// Get teachers for dropdown/list selection
        /// </summary>
        public static DataTable GetTeachersForDropdown(string connectionString)
        {
            using (var cn = new OdbcConnection(connectionString))
            {
                cn.Open();
                var cmd = cn.CreateCommand();
                cmd.CommandText = "SELECT Id, Name, Subject FROM Teachers ORDER BY Name";

                var dt = new DataTable();
                using (var da = new OdbcDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
                return dt;
            }
        }
        #endregion

        #region Private Helper Methods
        private static ETeacher DataTableToTeacher(DataTable dt)
        {
            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new ETeacher
            {
                Id = row.Field<int?>("Id"),
                Name = row.Field<string>("Name"),
                Subject = row.Field<string>("Subject"),
                Experience = row.Field<int?>("Experience")
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
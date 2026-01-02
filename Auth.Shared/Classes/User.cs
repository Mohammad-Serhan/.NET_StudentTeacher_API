using System.Data;
using System.Data.Odbc;
using System.Text;

namespace Auth.Shared.Classes
{
    public class EUser
    {
        public int? Id { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Email { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }

    public class CUser
    {
        #region Select Methods
        public static DataTable SelectAllDT_Odbc(EUser eUser, string odbcConnectionString)
        {
            using (var cn = new OdbcConnection(odbcConnectionString))
            {
                cn.Open();
                return SelectAllDT_Odbc(eUser, cn);
            }
        }

        public static DataTable SelectAllDT_Odbc(EUser eUser, OdbcConnection odbcConnection, OdbcTransaction tx = null)
        {
            var sb = new StringBuilder();
            var dt = new DataTable();
            var cmd = odbcConnection.CreateCommand();
            cmd.Transaction = tx;

            sb.Append("SELECT * FROM Users WHERE 1=1");
            var ands = new List<string>();
            var parameters = new List<OdbcParameter>();

            if (eUser != null)
            {
                AddEqIfHasValue("Id", eUser.Id, ands, parameters);
                AddEqIfHasValue("Username", eUser.Username, ands, parameters);
                AddEqIfHasValue("Email", eUser.Email, ands, parameters);
                AddEqIfHasValue("IsActive", eUser.IsActive, ands, parameters);
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

        public static EUser SelectById_Odbc(int userId, string odbcConnectionString)
        {
            var filter = new EUser { Id = userId };
            var dt = SelectAllDT_Odbc(filter, odbcConnectionString);
            return DataTableToUser(dt);
        }


        public static EUser SelectByUsername(string username, string odbcConnectionString)
        {
            var filter = new EUser { Username = username, IsActive = true }; // ✅ Add IsActive filter
            var dt = SelectAllDT_Odbc(filter, odbcConnectionString);
            return DataTableToUser(dt);
        }
        #endregion

        #region CRUD Operations


        public static int InsertUser(EUser user, string odbcConnectionString)
        {
            using (var cn = new OdbcConnection(odbcConnectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO Users (Username, Password, Email, IsActive, CreatedDate) VALUES (?, ?, ?, ?, ?)";
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = (object)user.Username ?? DBNull.Value });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = (object)user.Password ?? DBNull.Value });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = (object)user.Email ?? DBNull.Value });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Bit, Value = (object)user.IsActive ?? true });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.DateTime, Value = DateTime.UtcNow });
                    return cmd.ExecuteNonQuery();
                }
            }
        }



        public static int UpdateUser(EUser user, string odbcConnectionString)
        {
            using (var cn = new OdbcConnection(odbcConnectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE Users SET Username = ?, Email = ?, IsActive = ? WHERE Id = ?";
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = (object)user.Username ?? DBNull.Value });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = (object)user.Email ?? DBNull.Value });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Bit, Value = (object)user.IsActive ?? DBNull.Value });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = user.Id ?? (object)DBNull.Value });
                    return cmd.ExecuteNonQuery();
                }
            }
        }



        public static int UpdateUserPassword(int userId, string newPassword, string odbcConnectionString)
        {
            using (var cn = new OdbcConnection(odbcConnectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE Users SET Password = ? WHERE Id = ?";
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = newPassword });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = userId });
                    return cmd.ExecuteNonQuery();
                }
            }

        }


        public static int UpdateLastLogin(int userId, string odbcConnectionString)
        {
            using (var cn = new OdbcConnection(odbcConnectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE Users SET LastLoginDate = ? WHERE Id = ?";
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.DateTime, Value = DateTime.UtcNow });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = userId });
                    return cmd.ExecuteNonQuery();
                }
            }
        }
        #endregion

        #region Utility Methods
        private static EUser DataTableToUser(DataTable dt)
        {
            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new EUser
            {
                Id = row.Field<int?>("Id"),
                Username = row.Field<string>("Username"),
                Password = row.Field<string>("Password"),
                Email = row.Field<string>("Email"),
                IsActive = row.Field<bool?>("IsActive"),
                CreatedDate = row.Field<DateTime?>("CreatedDate"),
                LastLoginDate = row.Field<DateTime?>("LastLoginDate")
            };
        }

        public static bool UserExists(int? id, string odbcConnectionString)
        {
            var userByIdOnly = new EUser { Id = id };
            var dt = SelectAllDT_Odbc(userByIdOnly, odbcConnectionString);
            return dt.Rows.Count > 0;
        }

        public static bool UserExists(string username, string odbcConnectionString, int? excludeId = null)
        {
            var filter = new EUser { Username = username };
            var dt = SelectAllDT_Odbc(filter, odbcConnectionString);

            if (excludeId.HasValue)
            {
                return dt.AsEnumerable()
                    .Any(row => row.Field<int>("Id") != excludeId.Value);
            }

            return dt.Rows.Count > 0;
        }
        #endregion


        #region Select Methods
        public static DataTable SelectWithFilters(
            string connectionString,
            int? id = null,
            string? username = null,
            string? email = null,
            bool? isActive = null,
            DateTime? createdDateFrom = null,
            DateTime? createdDateTo = null,
            DateTime? lastLoginFrom = null,
            DateTime? lastLoginTo = null,
            int page = 1,
            int pageSize = 10,
            string? sortBy = "Username",
            bool sortDescending = false)
        {
            using (var cn = new OdbcConnection(connectionString))
            {
                cn.Open();
                var sb = new StringBuilder("SELECT * FROM Users WHERE 1=1");
                var parameters = new List<OdbcParameter>();
                var conditions = new List<string>();

                // Apply filters
                if (id.HasValue)
                {
                    conditions.Add("id == ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = id.Value });
                }
                if (!string.IsNullOrWhiteSpace(username))
                {
                    conditions.Add("Username LIKE ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = $"%{username}%" });
                }
                if (!string.IsNullOrWhiteSpace(email))
                {
                    conditions.Add("Email LIKE ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = $"%{email}%" });
                }
                if (isActive.HasValue)
                {
                    conditions.Add("IsActive = ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.Bit, Value = isActive.Value });
                }
                if (createdDateFrom.HasValue)
                {
                    conditions.Add("CreatedDate >= ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.DateTime, Value = createdDateFrom.Value });
                }
                if (createdDateTo.HasValue)
                {
                    conditions.Add("CreatedDate <= ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.DateTime, Value = createdDateTo.Value });
                }
                if (lastLoginFrom.HasValue)
                {
                    conditions.Add("LastLoginDate >= ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.DateTime, Value = lastLoginFrom.Value });
                }
                if (lastLoginTo.HasValue)
                {
                    conditions.Add("LastLoginDate <= ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.DateTime, Value = lastLoginTo.Value });
                }

                if (conditions.Count > 0) sb.Append(" AND ").Append(string.Join(" AND ", conditions));

                // Apply sorting
                var validSortColumns = new[] { "Id", "Username", "Email", "IsActive", "CreatedDate", "LastLoginDate" };
                var sortColumn = validSortColumns.Contains(sortBy) ? sortBy : "Username";
                var sortDirection = sortDescending ? "DESC" : "ASC";
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
            string? username = null,
            string? email = null,
            bool? isActive = null,
            DateTime? createdDateFrom = null,
            DateTime? createdDateTo = null,
            DateTime? lastLoginFrom = null,
            DateTime? lastLoginTo = null)
        {
            using (var cn = new OdbcConnection(connectionString))
            {
                cn.Open();
                var sb = new StringBuilder("SELECT COUNT(*) FROM Users WHERE 1=1");
                var parameters = new List<OdbcParameter>();
                var conditions = new List<string>();

                // Apply filters (same as SelectWithFilters)
                if (id.HasValue)
                {
                    conditions.Add("id == ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = id.Value });
                }
                if (!string.IsNullOrWhiteSpace(username))
                {
                    conditions.Add("Username LIKE ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = $"%{username}%" });
                }
                if (!string.IsNullOrWhiteSpace(email))
                {
                    conditions.Add("Email LIKE ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = $"%{email}%" });
                }
                if (isActive.HasValue)
                {
                    conditions.Add("IsActive = ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.Bit, Value = isActive.Value });
                }
                if (createdDateFrom.HasValue)
                {
                    conditions.Add("CreatedDate >= ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.DateTime, Value = createdDateFrom.Value });
                }
                if (createdDateTo.HasValue)
                {
                    conditions.Add("CreatedDate <= ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.DateTime, Value = createdDateTo.Value });
                }
                if (lastLoginFrom.HasValue)
                {
                    conditions.Add("LastLoginDate >= ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.DateTime, Value = lastLoginFrom.Value });
                }
                if (lastLoginTo.HasValue)
                {
                    conditions.Add("LastLoginDate <= ?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.DateTime, Value = lastLoginTo.Value });
                }

                if (conditions.Count > 0) sb.Append(" AND ").Append(string.Join(" AND ", conditions));

                var cmd = cn.CreateCommand();
                cmd.CommandText = sb.ToString();
                foreach (var p in parameters) cmd.Parameters.Add(p);

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
        #endregion



        #region User Role Management Functions
        public static List<ERole> GetUserRoles(int userId, string connectionString)
        {
            var roles = new List<ERole>();

            using (var cn = new OdbcConnection(connectionString))
            {
                cn.Open();
                var cmd = cn.CreateCommand();
                cmd.CommandText = @"
                    SELECT r.Id, r.RoleName, r.Description
                    FROM Roles r
                    INNER JOIN UserRoles ur ON r.Id = ur.RoleId
                    WHERE ur.UserId = ?";
                cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = userId });

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        roles.Add(new ERole
                        {
                            Id = reader["Id"] as int?,
                            RoleName = reader["RoleName"] as string,
                            Description = reader["Description"] as string,
                        });
                    }
                }
            }

            return roles;
        }

        public static bool UpdateUserRoles(int userId, List<int> roleIds, string connectionString)
        {
            using (var cn = new OdbcConnection(connectionString))
            {
                cn.Open();
                using (var transaction = cn.BeginTransaction())
                {
                    try
                    {
                        // Remove existing roles
                        var deleteCmd = cn.CreateCommand();
                        deleteCmd.Transaction = transaction;
                        deleteCmd.CommandText = "DELETE FROM UserRoles WHERE UserId = ?";
                        deleteCmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = userId });
                        deleteCmd.ExecuteNonQuery();

                        // Add new roles
                        foreach (var roleId in roleIds)
                        {
                            var insertCmd = cn.CreateCommand();
                            insertCmd.Transaction = transaction;
                            insertCmd.CommandText = "INSERT INTO UserRoles (UserId, RoleId) VALUES (?, ?)";
                            insertCmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = userId });
                            insertCmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = roleId });
                            insertCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
        #endregion


        #region User Permission Methods

        /// <summary>
        /// Gets all permissions for a specific user by ID
        /// </summary>
        public static List<string> GetUserPermissions(int userId, string connectionString)
        {
            var permissions = new List<string>();

            using (var cn = new OdbcConnection(connectionString))
            {
                cn.Open();
                var cmd = cn.CreateCommand();

                // ✅ Check both user is active and get permissions
                cmd.CommandText = @"
            SELECT DISTINCT p.ActionName 
            FROM Permissions p
            INNER JOIN RolePermissions rp ON p.Id = rp.PermissionId
            INNER JOIN UserRoles ur ON rp.RoleId = ur.RoleId
            INNER JOIN Users u ON ur.UserId = u.Id
            WHERE u.Id = ? AND u.IsActive = 1";

                cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = userId });

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var permission = reader["ActionName"] as string;
                        if (!string.IsNullOrEmpty(permission))
                        {
                            permissions.Add(permission);
                        }
                    }
                }
            }

            return permissions;
        }

        /// <summary>
        /// Checks if a user has a specific permission
        /// </summary>
        public static bool UserHasPermission(int userId, string permission, string connectionString)
        {
            using (var cn = new OdbcConnection(connectionString))
            {
                cn.Open();
                var cmd = cn.CreateCommand();

                cmd.CommandText = @"
            SELECT COUNT(*) 
            FROM Permissions p
            INNER JOIN RolePermissions rp ON p.Id = rp.PermissionId
            INNER JOIN UserRoles ur ON rp.RoleId = ur.RoleId
            INNER JOIN Users u ON ur.UserId = u.Id
            WHERE u.Id = ? AND u.IsActive = 1 AND p.ActionName = ?";

                cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = userId });
                cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = permission });

                var count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
        }

        /// <summary>
        /// Checks if a user has all specified permissions
        /// </summary>
        public static bool UserHasAllPermissions(int userId, List<string> permissions, string connectionString)
        {
            if (permissions == null || !permissions.Any())
                return true;

            using (var cn = new OdbcConnection(connectionString))
            {
                cn.Open();
                var cmd = cn.CreateCommand();

                var parameters = new List<OdbcParameter>();
                var permissionPlaceholders = new List<string>();

                for (int i = 0; i < permissions.Count; i++)
                {
                    permissionPlaceholders.Add("?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = permissions[i] });
                }

                cmd.CommandText = $@"
            SELECT COUNT(DISTINCT p.ActionName) 
            FROM Permissions p
            INNER JOIN RolePermissions rp ON p.Id = rp.PermissionId
            INNER JOIN UserRoles ur ON rp.RoleId = ur.RoleId
            INNER JOIN Users u ON ur.UserId = u.Id
            WHERE u.Id = ? AND u.IsActive = 1 AND p.ActionName IN ({string.Join(",", permissionPlaceholders)})";

                cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = userId });
                foreach (var param in parameters)
                {
                    cmd.Parameters.Add(param);
                }

                var count = Convert.ToInt32(cmd.ExecuteScalar());
                return count == permissions.Count;
            }
        }

        /// <summary>
        /// Checks if a user has any of the specified permissions
        /// </summary>
        public static bool UserHasAnyPermission(int userId, List<string> permissions, string connectionString)
        {
            if (permissions == null || !permissions.Any())
                return false;

            using (var cn = new OdbcConnection(connectionString))
            {
                cn.Open();
                var cmd = cn.CreateCommand();

                var parameters = new List<OdbcParameter>();
                var permissionPlaceholders = new List<string>();

                for (int i = 0; i < permissions.Count; i++)
                {
                    permissionPlaceholders.Add("?");
                    parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = permissions[i] });
                }

                cmd.CommandText = $@"
            SELECT COUNT(*) 
            FROM Permissions p
            INNER JOIN RolePermissions rp ON p.Id = rp.PermissionId
            INNER JOIN UserRoles ur ON rp.RoleId = ur.RoleId
            INNER JOIN Users u ON ur.UserId = u.Id
            WHERE u.Id = ? AND u.IsActive = 1 AND p.ActionName IN ({string.Join(",", permissionPlaceholders)})";

                cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = userId });
                foreach (var param in parameters)
                {
                    cmd.Parameters.Add(param);
                }

                var count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
        }

        #endregion

        #region Parameter Helper Methods
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

        private static void AddEqIfHasValue(string column, bool? value, List<string> ands, List<OdbcParameter> parameters)
        {
            if (value.HasValue)
            {
                ands.Add($"{column} = ?");
                parameters.Add(new OdbcParameter { OdbcType = OdbcType.Bit, Value = value.Value });
            }
        }
        #endregion
    }
}
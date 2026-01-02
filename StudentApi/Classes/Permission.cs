using System.Data;
using System.Data.Odbc;
using System.Text;

namespace StudentApi.Classes
{
    public class EPermission
    {
        public int? Id { get; set; }
        public string? ActionName { get; set; }
        public string? Description { get; set; }
    }

    public class CPermission
    {
        #region Select Methods
        public static DataTable SelectAllDT(EPermission ePermission, string odbcConnectionString)
        {
            using (var cn = new OdbcConnection(odbcConnectionString))
            {
                cn.Open();
                return SelectAllDT(ePermission, cn);
            }
        }

        public static DataTable SelectAllDT(EPermission ePermission, OdbcConnection odbcConnection, OdbcTransaction tx = null)
        {
            var sb = new StringBuilder();
            var dt = new DataTable();
            var cmd = odbcConnection.CreateCommand();
            cmd.Transaction = tx;

            sb.Append("SELECT * FROM Permissions WHERE 1=1");
            var ands = new List<string>();
            var parameters = new List<OdbcParameter>();

            if (ePermission != null)
            {
                AddEqIfHasValue("Id", ePermission.Id, ands, parameters);
                AddEqIfHasValue("ActionName", ePermission.ActionName, ands, parameters);
                AddEqIfHasValue("Description", ePermission.Description, ands, parameters);
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

        public static List<EPermission> SelectAllList(EPermission ePermission, string odbcConnectionString)
        {
            var dt = SelectAllDT(ePermission, odbcConnectionString);
            return DataTableToList(dt);
        }


        public static EPermission SelectById(int permissionId, string odbcConnectionString)
        {
            var filter = new EPermission { Id = permissionId };
            var dt = SelectAllDT(filter, odbcConnectionString);
            return DataTableToPermission(dt);
        }
        #endregion

        #region CRUD Operations

        public static int Insert(EPermission permission, string odbcConnectionString)
        {
            using (var cn = new OdbcConnection(odbcConnectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO Permissions (ActionName, Description) VALUES (?, ?)";
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = (object)permission.ActionName ?? DBNull.Value });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = (object)permission.Description ?? DBNull.Value });
                    return cmd.ExecuteNonQuery();
                }
            }
        }


        

        public static int Update(EPermission permission, string odbcConnectionString)
        {
            using (var cn = new OdbcConnection(odbcConnectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE Permissions SET ActionName = ?, Description = ? WHERE Id = ?";
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = (object)permission.ActionName ?? DBNull.Value });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = (object)permission.Description ?? DBNull.Value });
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = permission.Id ?? (object)DBNull.Value });
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        

        public static int Delete(int id, string odbcConnectionString)
        {
            using (var cn = new OdbcConnection(odbcConnectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Permissions WHERE Id = ?";
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = id });
                    return cmd.ExecuteNonQuery();
                }
            }
        }
        #endregion



        #region Utility Methods
        public static bool PermissionExists(int? id, string odbcConnectionString)
        {
            var permissionByIdOnly = new EPermission { Id = id };
            var dt = SelectAllDT(permissionByIdOnly, odbcConnectionString);
            return dt.Rows.Count > 0;
        }


        public static bool ActionNameExists(string actionName, string odbcConnectionString, int? excludeId = null)
        {
            var filter = new EPermission { ActionName = actionName };
            var dt = SelectAllDT(filter, odbcConnectionString);

            if (excludeId.HasValue)
            {
                return dt.AsEnumerable()
                    .Any(row => row.Field<int>("Id") != excludeId.Value);
            }

            return dt.Rows.Count > 0;
        }


        public static List<EPermission> GetAllPermissions(string connectionString)
        {
            return SelectAllList(new EPermission(), connectionString);
        }


        public static EPermission GetPermissionById(int permissionId, string connectionString)
        {
            return SelectById(permissionId, connectionString);
        }


        public static bool PermissionExists(string actionName, string connectionString, int? excludePermissionId = null)
        {
            return ActionNameExists(actionName, connectionString, excludePermissionId);
        }
        #endregion



        #region Private Helper Methods
        private static List<EPermission> DataTableToList(DataTable dt)
        {
            var permissions = new List<EPermission>();
            foreach (DataRow row in dt.Rows)
            {
                permissions.Add(new EPermission
                {
                    Id = row.Field<int?>("Id"),
                    ActionName = row.Field<string>("ActionName"),
                    Description = row.Field<string>("Description")
                });
            }
            return permissions;
        }

        private static EPermission DataTableToPermission(DataTable dt)
        {
            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new EPermission
            {
                Id = row.Field<int?>("Id"),
                ActionName = row.Field<string>("ActionName"),
                Description = row.Field<string>("Description")
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
        #endregion
    }
}
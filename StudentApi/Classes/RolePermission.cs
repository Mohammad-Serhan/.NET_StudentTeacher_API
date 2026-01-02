using System.Data;
using System.Data.Odbc;
using System.Text;
using StudentApi.DTO;

namespace StudentApi.Classes
{
    public class ERolePermission
    {
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
    }

    public class CRolePermission
    {
        #region Select Methods
        public static DataTable SelectAllDT_Odbc(ERolePermission eRolePermission, string odbcConnectionString)
        {
            using (var cn = new OdbcConnection(odbcConnectionString))
            {
                cn.Open();
                return SelectAllDT_Odbc(eRolePermission, cn);
            }
        }

        public static DataTable SelectAllDT_Odbc(ERolePermission eRolePermission, OdbcConnection odbcConnection, OdbcTransaction tx = null)
        {
            var sb = new StringBuilder();
            var dt = new DataTable();
            var cmd = odbcConnection.CreateCommand();
            cmd.Transaction = tx;

            sb.Append("SELECT * FROM RolePermissions WHERE 1=1");
            var ands = new List<string>();
            var parameters = new List<OdbcParameter>();

            if (eRolePermission != null)
            {
                AddEqIfHasValue("RoleId", eRolePermission.RoleId, ands, parameters);
                AddEqIfHasValue("PermissionId", eRolePermission.PermissionId, ands, parameters);
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



        public static DataTable GetRolePermissionsWithDetails(int roleId, string odbcConnectionString)
        {
            using (var cn = new OdbcConnection(odbcConnectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {

                    cmd.CommandText = @"
                    SELECT rp.RoleId, rp.PermissionId, p.ActionName, p.Description 
                    FROM RolePermissions rp
                    INNER JOIN Permissions p ON rp.PermissionId = p.Id
                    WHERE rp.RoleId = ?";
                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = roleId });

                    var dt = new DataTable();
                    using (var da = new OdbcDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                    return dt;
                }
            }
        }

        public static List<PermissionDetailDTO> GetPermissionDetailsForRole(int roleId, string odbcConnectionString)
        {
            try
            {
                using (var cn = new OdbcConnection(odbcConnectionString))
                {
                    cn.Open();
                    using (var cmd = cn.CreateCommand())
                    {
                        cmd.CommandText = @"
                    SELECT 
                        p.Id as PermissionId,
                        p.ActionName
                    FROM RolePermissions rp
                    INNER JOIN Permissions p ON rp.PermissionId = p.Id
                    WHERE rp.RoleId = ? 
                    ORDER BY p.ActionName";

                        cmd.Parameters.Add(new OdbcParameter
                        {
                            ParameterName = "RoleId",
                            OdbcType = OdbcType.Int,
                            Value = roleId
                        });

                        var permissionDetails = new List<PermissionDetailDTO>();
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                try
                                {
                                    if (!reader.IsDBNull(0))
                                    {
                                        var detail = new PermissionDetailDTO
                                        {
                                            PermissionId = reader.GetInt32(0),
                                            ActionName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)
                                        };
                                        permissionDetails.Add(detail);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error reading permission details: {ex.Message}");
                                }
                            }
                        }

                        Console.WriteLine($"Found {permissionDetails.Count} permissions for role {roleId}");
                        foreach (var detail in permissionDetails)
                        {
                            Console.WriteLine($"  - PermissionId: {detail.PermissionId}, ActionName: {detail.ActionName}");
                        }

                        return permissionDetails;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetPermissionDetailsForRole for role {roleId}: {ex.Message}");
                return new List<PermissionDetailDTO>();
            }
        }

        #endregion

        #region CRUD Operations
        public static int InsertRolePermission(ERolePermission rolePermission, string odbcConnectionString)
        {
            using (var cn = new OdbcConnection(odbcConnectionString))
            {
                cn.Open();
                return InsertRolePermission(rolePermission, cn);
            }
        }

        public static int InsertRolePermission(ERolePermission rolePermission, OdbcConnection odbcConnection, OdbcTransaction tx = null)
        {
            using (var cmd = odbcConnection.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "INSERT INTO RolePermissions (RoleId, PermissionId) VALUES (?, ?)";
                cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = rolePermission.RoleId });
                cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = rolePermission.PermissionId });
                return cmd.ExecuteNonQuery();
            }
        }

        public static int DeleteRolePermission(int roleId, int permissionId, string odbcConnectionString)
        {
            using (var cn = new OdbcConnection(odbcConnectionString))
            {
                cn.Open();
                return DeleteRolePermission(roleId, permissionId, cn);
            }
        }

        public static int DeleteRolePermission(int roleId, int permissionId, OdbcConnection odbcConnection, OdbcTransaction tx = null)
        {
            using (var cmd = odbcConnection.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM RolePermissions WHERE RoleId = ? AND PermissionId = ?";
                cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = roleId });
                cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = permissionId });
                return cmd.ExecuteNonQuery();
            }
        }

        public static int DeleteAllRolePermissions(int roleId, string odbcConnectionString)
        {
            using (var cn = new OdbcConnection(odbcConnectionString))
            {
                cn.Open();
                return DeleteAllRolePermissions(roleId, cn);
            }
        }

        public static int DeleteAllRolePermissions(int roleId, OdbcConnection odbcConnection, OdbcTransaction tx = null)
        {
            using (var cmd = odbcConnection.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM RolePermissions WHERE RoleId = ?";
                cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = roleId });
                return cmd.ExecuteNonQuery();
            }
        }
        #endregion

        #region Bulk Operations
        public static AssignPermissionsResult AssignPermissionsToRole(int roleId, List<int> permissionIds, string odbcConnectionString)
        {
            var result = new AssignPermissionsResult();

            using (var cn = new OdbcConnection(odbcConnectionString))
            {
                cn.Open();
                using (var transaction = cn.BeginTransaction())
                {
                    try
                    {
                        // Remove existing permissions
                        DeleteAllRolePermissions(roleId, cn, transaction);

                        // Add new permissions
                        foreach (var permissionId in permissionIds.Distinct())
                        {
                            var rolePermission = new ERolePermission { RoleId = roleId, PermissionId = permissionId };
                            int affected = InsertRolePermission(rolePermission, cn, transaction);
                            result.RowsAffected += affected;
                        }

                        transaction.Commit();
                        result.Success = true;
                        result.Message = "Permissions assigned successfully";
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        result.Success = false;
                        result.ErrorMessage = ex.Message;
                    }
                }
            }
            return result;
        }
        #endregion

        #region Utility Methods
        public static bool RolePermissionExists(int roleId, int permissionId, string odbcConnectionString)
        {
            var filter = new ERolePermission { RoleId = roleId, PermissionId = permissionId };
            var dt = SelectAllDT_Odbc(filter, odbcConnectionString);
            return dt.Rows.Count > 0;
        }
        #endregion

        #region Parameter Helper Methods
        private static void AddEqIfHasValue(string column, int value, List<string> ands, List<OdbcParameter> parameters)
        {
            ands.Add($"{column} = ?");
            parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = value });
        }
        #endregion
    }


}
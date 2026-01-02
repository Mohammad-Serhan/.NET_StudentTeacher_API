//using System.Data;
//using System.Data.Odbc;
//using System.Text;

//namespace StudentApi.Classes
//{
//    public class ERole
//    {
//        public int? Id { get; set; }
//        public string? RoleName { get; set; }
//        public string? Description { get; set; }
//    }

//    public class CRole
//    {
//        #region Select Methods
//        public static DataTable SelectAllDT_Odbc(ERole eRole, string odbcConnectionString)
//        {
//            using (var cn = new OdbcConnection(odbcConnectionString))
//            {
//                cn.Open();
//                return SelectAllDT_Odbc(eRole, cn);
//            }
//        }

//        public static DataTable SelectAllDT_Odbc(ERole eRole, OdbcConnection odbcConnection, OdbcTransaction tx = null)
//        {
//            var sb = new StringBuilder();
//            var dt = new DataTable();
//            var cmd = odbcConnection.CreateCommand();
//            cmd.Transaction = tx;

//            sb.Append("SELECT * FROM Roles WHERE 1=1");
//            var ands = new List<string>();
//            var parameters = new List<OdbcParameter>();

//            if (eRole != null)
//            {
//                AddEqIfHasValue("Id", eRole.Id, ands, parameters);
//                AddEqIfHasValue("RoleName", eRole.RoleName, ands, parameters);
//                AddEqIfHasValue("Description", eRole.Description, ands, parameters);
//            }

//            foreach (var a in ands) sb.Append(" AND ").Append(a);

//            cmd.CommandText = sb.ToString();
//            foreach (var p in parameters) cmd.Parameters.Add(p);

//            using (var da = new OdbcDataAdapter(cmd))
//            {
//                da.Fill(dt);
//            }
//            return dt;
//        }

//        /// <summary>
//        /// Gets all roles from the database
//        /// </summary>
//        public static DataTable GetAllRoles(string connectionString)
//        {
//            return SelectAllDT_Odbc(null, connectionString);
//        }

//        /// <summary>
//        /// Get role by ID
//        /// </summary>
//        public static ERole GetRoleById(int roleId, string connectionString)
//        {
//            var filter = new ERole { Id = roleId };
//            var dt = SelectAllDT_Odbc(filter, connectionString);
//            return DataTableToRole(dt);
//        }

//        /// <summary>
//        /// Get role by name
//        /// </summary>
//        public static ERole GetRoleByName(string roleName, string connectionString)
//        {
//            var filter = new ERole { RoleName = roleName };
//            var dt = SelectAllDT_Odbc(filter, connectionString);
//            return DataTableToRole(dt);
//        }
//        #endregion

//        #region CRUD Operations
//        public static int InsertRole(ERole role, string odbcConnectionString)
//        {
//            using (var cn = new OdbcConnection(odbcConnectionString))
//            {
//                cn.Open();
//                using (var cmd = cn.CreateCommand())
//                {
//                    cmd.CommandText = "INSERT INTO Roles (RoleName, Description) VALUES (?, ?)";
//                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = role.RoleName });
//                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = role.Description });
//                    return cmd.ExecuteNonQuery();
//                }
//            }
//        }

//        public static int UpdateRole(ERole role, string odbcConnectionString)
//        {
//            using (var cn = new OdbcConnection(odbcConnectionString))
//            {
//                cn.Open();
//                using (var cmd = cn.CreateCommand())
//                {
//                    cmd.CommandText = "UPDATE Roles SET RoleName = ?, Description = ? WHERE Id = ?";
//                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = role.RoleName  });
//                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = role.Description  });
//                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = role.Id  });
//                    return cmd.ExecuteNonQuery();
//                }
//            }
//        }

//        public static int DeleteRole(int Id, string odbcConnectionString)
//        {
//            using (var cn = new OdbcConnection(odbcConnectionString))
//            {
//                cn.Open();
//                using (var cmd = cn.CreateCommand())
//                {
//                    cmd.CommandText = "DELETE FROM Roles WHERE Id = ?";
//                    cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = Id });
//                    return cmd.ExecuteNonQuery();
//                }
//            }
//        }
//        #endregion

//        #region Utility Methods
//        public static bool RoleExists(int? id, string odbcConnectionString)
//        {
//            var roleByIdOnly = new ERole { Id = id };
//            var dt = SelectAllDT_Odbc(roleByIdOnly, odbcConnectionString);
//            return dt.Rows.Count > 0;
//        }

//        public static bool RoleNameExists(string roleName, string odbcConnectionString, int? excludeId = null)
//        {
//            var filter = new ERole { RoleName = roleName };
//            var dt = SelectAllDT_Odbc(filter, odbcConnectionString);

//            if (excludeId.HasValue)
//            {
//                return dt.AsEnumerable()
//                    .Any(row => row.Field<int>("Id") != excludeId.Value);
//            }

//            return dt.Rows.Count > 0;
//        }
//        #endregion

//        #region User Role Management
//        public static List<EUser> GetUserRolesByRoleId(int roleId, string connectionString)
//        {
//            var users = new List<EUser>();

//            using (var cn = new OdbcConnection(connectionString))
//            {
//                cn.Open();
//                var cmd = cn.CreateCommand();
//                cmd.CommandText = @"
//                SELECT u.Id, u.Username, u.Email, u.IsActive, u.CreatedDate, u.LastLoginDate
//                FROM Users u
//                INNER JOIN UserRoles ur ON u.Id = ur.UserId
//                WHERE ur.RoleId = ?";
//                cmd.Parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = roleId });

//                using (var reader = cmd.ExecuteReader())
//                {
//                    while (reader.Read())
//                    {
//                        users.Add(new EUser
//                        {
//                            Id = reader["Id"] as int?,
//                            Username = reader["Username"] as string,
//                            Email = reader["Email"] as string,
//                            IsActive = reader["IsActive"] as bool?,
//                            CreatedDate = reader["CreatedDate"] as DateTime?,
//                            LastLoginDate = reader["LastLoginDate"] as DateTime?
//                        });
//                    }
//                }
//            }

//            return users;
//        }
//        #endregion

//        #region Private Helper Methods
//        private static ERole DataTableToRole(DataTable dt)
//        {
//            if (dt.Rows.Count == 0) return null;

//            var row = dt.Rows[0];
//            return new ERole
//            {
//                Id = row.Field<int?>("Id"),
//                RoleName = row.Field<string>("RoleName"),
//                Description = row.Field<string>("Description")
//            };
//        }

//        private static void AddEqIfHasValue(string column, int? value, List<string> ands, List<OdbcParameter> parameters)
//        {
//            if (value.HasValue)
//            {
//                ands.Add($"{column} = ?");
//                parameters.Add(new OdbcParameter { OdbcType = OdbcType.Int, Value = value.Value });
//            }
//        }

//        private static void AddEqIfHasValue(string column, string value, List<string> ands, List<OdbcParameter> parameters)
//        {
//            if (!string.IsNullOrWhiteSpace(value))
//            {
//                ands.Add($"{column} = ?");
//                parameters.Add(new OdbcParameter { OdbcType = OdbcType.NVarChar, Value = value });
//            }
//        }
//        #endregion
//    }
//}
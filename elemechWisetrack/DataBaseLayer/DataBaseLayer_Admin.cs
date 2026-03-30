using Npgsql;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLayer_Admin
    {
        Task<object> ApproveVendor(string userId, string adminEmail);
        Task<object> RejectVendor(string userId, string adminEmail, string reason);
        Task<object> GetAllVendors();
        Task<object> GetAllUsersByRole();

    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Admin { }

    public partial class DataBaseLayer
    {
        public async Task<object> ApproveVendor(string userId, string adminEmail)
        {
            try
            {
                using (var conn = new NpgsqlConnection(DbConnection))
                {
                    await conn.OpenAsync();

                    string query = @"
                UPDATE ""AspNetUsers""
                SET 
                    ""Status"" = @status,
                    ""ApprovedBy"" = @approvedBy,
                    ""ApprovedAt"" = @approvedAt
                WHERE ""Id"" = @userId
            ";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@status", "approved");
                        cmd.Parameters.AddWithValue("@approvedBy", (object?)adminEmail ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@approvedAt", DateTime.UtcNow);
                        cmd.Parameters.AddWithValue("@userId", userId);

                        int rows = await cmd.ExecuteNonQueryAsync();

                        if (rows == 0)
                        {
                            return new { success = false, message = "Vendor not found" };
                        }
                    }
                }

                return new { success = true, message = "Vendor approved successfully" };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        public async Task<object> RejectVendor(string userId, string adminEmail, string reason)
        {
            try
            {
                using (var conn = new NpgsqlConnection(DbConnection))
                {
                    await conn.OpenAsync();

                    string query = @"
                UPDATE ""AspNetUsers""
                SET 
                    ""Status"" = @status,
                    ""ApprovedBy"" = @approvedBy,
                    ""ApprovedAt"" = @approvedAt,
                    ""RejectionReason"" = @reason
                WHERE ""Id"" = @userId
            ";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@status", "rejected");
                        cmd.Parameters.AddWithValue("@approvedBy", (object?)adminEmail ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@approvedAt", DateTime.UtcNow);
                        cmd.Parameters.AddWithValue("@reason", (object?)reason ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@userId", userId);

                        int rows = await cmd.ExecuteNonQueryAsync();

                        if (rows == 0)
                        {
                            return new { success = false, message = "Vendor not found" };
                        }
                    }
                }

                return new { success = true, message = "Vendor rejected successfully" };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        public async Task<object> GetAllVendors()
        {
            try
            {
                using (var conn = new NpgsqlConnection(DbConnection))
                {
                    await conn.OpenAsync();

                    string query = @"
                SELECT 
                    ""Id"",
                    ""Email"",
                    ""UserName"",
                    ""FirstName"",
                    ""LastName"",
                    ""PhoneNumber"",
                    ""IsActive"",
                    ""CreateDate"",
                    ""AccessKey"",
                    ""Lastotp"",
                    ""PasswordChangeTime"",
                    ""PasswordChangeBy"",
                    ""OrgId"",
                    ""userType"",
                    ""signupsource"",
                    ""sourcetype"",
                    ""address"",
                    ""IsVendor"",
                    ""Status"",
                    ""ApprovedBy"",
                    ""ApprovedAt"",
                    ""RejectionReason""
                FROM ""AspNetUsers""
                WHERE ""sourcetype"" = 'vendor'
                ORDER BY ""CreateDate"" DESC
            ";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var list = new List<object>();

                        while (await reader.ReadAsync())
                        {
                            list.Add(new
                            {
                                Id = reader["Id"]?.ToString(),
                                Email = reader["Email"]?.ToString(),
                                UserName = reader["UserName"]?.ToString(),
                                FirstName = reader["FirstName"]?.ToString(),
                                LastName = reader["LastName"]?.ToString(),
                                PhoneNumber = reader["PhoneNumber"]?.ToString(),

                                IsActive = reader["IsActive"] != DBNull.Value && (bool)reader["IsActive"],
                                CreateDate = reader["CreateDate"] == DBNull.Value ? null : reader["CreateDate"],

                                AccessKey = reader["AccessKey"]?.ToString(),
                                Lastotp = reader["Lastotp"]?.ToString(),

                                PasswordChangeTime = reader["PasswordChangeTime"] == DBNull.Value ? null : reader["PasswordChangeTime"],
                                PasswordChangeBy = reader["PasswordChangeBy"]?.ToString(),

                                OrgId = reader["OrgId"]?.ToString(),
                                UserType = reader["userType"] == DBNull.Value ? null : reader["userType"],

                                SignupSource = reader["signupsource"]?.ToString(),
                                SourceType = reader["sourcetype"]?.ToString(),

                                Address = reader["address"]?.ToString(),

                                IsVendor = reader["IsVendor"] != DBNull.Value && (bool)reader["IsVendor"],
                                Status = reader["Status"]?.ToString(),

                                ApprovedBy = reader["ApprovedBy"]?.ToString(),
                                ApprovedAt = reader["ApprovedAt"] == DBNull.Value ? null : reader["ApprovedAt"],

                                RejectionReason = reader["RejectionReason"]?.ToString()
                            });
                        }

                        return new
                        {
                            success = true,
                            count = list.Count,
                            data = list
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }
        public async Task<object> GetAllUsersByRole()
        {
            try
            {
                using (var conn = new NpgsqlConnection(DbConnection))
                {
                    await conn.OpenAsync();

                    string query = @"
                SELECT 
                    u.""Id"",
                    u.""Email"",
                    u.""UserName"",
                    u.""FirstName"",
                    u.""LastName"",
                    u.""PhoneNumber"",
                    u.""IsActive"",
                    u.""CreateDate"",
                    r.""Name"" AS ""RoleName""
                FROM ""AspNetUsers"" u
                INNER JOIN ""AspNetUserRoles"" ur ON u.""Id"" = ur.""UserId""
                INNER JOIN ""AspNetRoles"" r ON ur.""RoleId"" = r.""Id""
                WHERE r.""Name"" = @role
                ORDER BY u.""CreateDate"" DESC
            ";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@role", "USER");

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            var list = new List<object>();

                            while (await reader.ReadAsync())
                            {
                                list.Add(new
                                {
                                    Id = reader["Id"]?.ToString(),
                                    Email = reader["Email"]?.ToString(),
                                    UserName = reader["UserName"]?.ToString(),
                                    FirstName = reader["FirstName"]?.ToString(),
                                    LastName = reader["LastName"]?.ToString(),
                                    PhoneNumber = reader["PhoneNumber"]?.ToString(),
                                    IsActive = reader["IsActive"] != DBNull.Value && (bool)reader["IsActive"],
                                    CreateDate = reader["CreateDate"] == DBNull.Value ? null : reader["CreateDate"],
                                    Role = reader["RoleName"]?.ToString()
                                });
                            }

                            return new
                            {
                                success = true,
                                count = list.Count,
                                data = list
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }
    }
}

using elemechWisetrack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;
using System.Net;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLayer_Address
    {
        Task<Guid> AddAddressAsync(Guid userId, AddressRequest request);
        Task<List<AddressDetails>> GetAddressesAsync(Guid userId);
        Task<AddressDetails> GetAddressByIdAsync(Guid id, Guid userId);
        Task<bool> UpdateAddressAsync(Guid id, Guid userId, AddressRequest request);
        Task<bool> DeleteAddressAsync(Guid id, Guid userId);
        Task<AppUser> GetUserByEmailAsync(string email);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Address
    {

    }

    public partial class DataBaseLayer
    {
        public async Task<AppUser> GetUserByEmailAsync(string email)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            string query = @"
        SELECT ""Id"", ""Email"", ""FirstName"", ""PasswordHash""
        FROM ""AspNetUsers""
        WHERE ""Email"" = @Email
        LIMIT 1;";

            using var cmd = new NpgsqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Email", email);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new AppUser
            {
                Id = Guid.Parse(reader["Id"].ToString()), // Identity Id is string
                Email = reader["Email"].ToString(),
                FirstName = reader["FirstName"]?.ToString(),
                Password = reader["PasswordHash"]?.ToString()
            };
        }


        public async Task<Guid> AddAddressAsync(Guid userId, AddressRequest request)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    Guid addressId = Guid.NewGuid();

                    string query = @"
                INSERT INTO address_details
                (
                    id,
                    user_id,
                    full_name,
                    phone_number,
                    address_line1,
                    address_line2,
                    city,
                    state,
                    country,
                    postal_code,
                    address_type,
                    is_default,
                    created_at,
                    updated_at
                )
                VALUES
                (
                    @Id,
                    @UserId,
                    @FullName,
                    @PhoneNumber,
                    @AddressLine1,
                    @AddressLine2,
                    @City,
                    @State,
                    @Country,
                    @PostalCode,
                    @AddressType,
                    @IsDefault,
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP
                );";

                    using (var cmd = new NpgsqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", addressId);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@FullName", request.FullName);
                        cmd.Parameters.AddWithValue("@PhoneNumber", (object?)request.PhoneNumber ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@AddressLine1", request.AddressLine1);
                        cmd.Parameters.AddWithValue("@AddressLine2", (object?)request.AddressLine2 ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@City", request.City);
                        cmd.Parameters.AddWithValue("@State", request.State);
                        cmd.Parameters.AddWithValue("@Country", request.Country ?? "India");
                        cmd.Parameters.AddWithValue("@PostalCode", request.PostalCode);
                        cmd.Parameters.AddWithValue("@AddressType", (object?)request.AddressType ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsDefault", request.IsDefault);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    return addressId;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while adding address", ex);
            }
        }



        public async Task<List<AddressDetails>> GetAddressesAsync(Guid userId)
        {
            var list = new List<AddressDetails>();

            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            string query = @"SELECT * FROM address_details WHERE user_id = @UserId";

            using var cmd = new NpgsqlCommand(query, con);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapAddress(reader));
            }

            return list;
        }

        public async Task<AddressDetails> GetAddressByIdAsync(Guid id, Guid userId)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            string query = @"SELECT * FROM address_details WHERE id = @Id AND user_id = @UserId";

            using var cmd = new NpgsqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return MapAddress(reader);
        }

        public async Task<bool> UpdateAddressAsync(Guid id, Guid userId, AddressRequest request)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            string query = @"
        UPDATE address_details SET
            full_name = @FullName,
            phone_number = @PhoneNumber,
            address_line1 = @AddressLine1,
            address_line2 = @AddressLine2,
            city = @City,
            state = @State,
            country = @Country,
            postal_code = @PostalCode,
            address_type = @AddressType,
            is_default = @IsDefault,
            updated_at = CURRENT_TIMESTAMP
        WHERE id = @Id AND user_id = @UserId";

            using var cmd = new NpgsqlCommand(query, con);

            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@FullName", request.FullName);
            cmd.Parameters.AddWithValue("@PhoneNumber", (object?)request.PhoneNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AddressLine1", request.AddressLine1);
            cmd.Parameters.AddWithValue("@AddressLine2", (object?)request.AddressLine2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@City", request.City);
            cmd.Parameters.AddWithValue("@State", request.State);
            cmd.Parameters.AddWithValue("@Country", request.Country ?? "India");
            cmd.Parameters.AddWithValue("@PostalCode", request.PostalCode);
            cmd.Parameters.AddWithValue("@AddressType", (object?)request.AddressType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsDefault", request.IsDefault);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteAddressAsync(Guid id, Guid userId)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            string query = @"DELETE FROM address_details WHERE id = @Id AND user_id = @UserId";

            using var cmd = new NpgsqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@UserId", userId);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        private AddressDetails MapAddress(IDataRecord r)
        {
            return new AddressDetails
            {
                Id = r.GetGuid(r.GetOrdinal("id")),
                UserId = r.GetGuid(r.GetOrdinal("user_id")),
                FullName = r["full_name"].ToString(),
                PhoneNumber = r["phone_number"]?.ToString(),
                AddressLine1 = r["address_line1"].ToString(),
                AddressLine2 = r["address_line2"]?.ToString(),
                City = r["city"].ToString(),
                State = r["state"].ToString(),
                Country = r["country"].ToString(),
                PostalCode = r["postal_code"].ToString(),
                AddressType = r["address_type"]?.ToString(),
                IsDefault = (bool)r["is_default"],
                CreatedAt = (DateTime)r["created_at"],
                UpdatedAt = (DateTime)r["updated_at"]
            };
        }

    }
}

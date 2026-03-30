using elemechWisetrack.Models;
using Npgsql;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLaye_PinCode
    {
        Task<object> AddPinCode(string userEmail, AddPincodeRequest model);
        Task<object> GetAllPinCodes();
        Task<object> CheckPincode(string pincode);
        Task<object> UpdatePinCode(Guid id, string userEmail, AddPincodeRequest model);
        Task<object> DeletePinCode(Guid id);
    }

    public partial interface IDataBaseLayer : IDataBaseLaye_PinCode { }

    public partial class DataBaseLayer
    {
        // ✅ ADD
        public async Task<object> AddPinCode(string userEmail, AddPincodeRequest model)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string query = @"INSERT INTO delivery_pincodes 
            (pincode, city, state, is_serviceable, delivery_days, created_by_email)
            VALUES (@pincode, @city, @state, @is_serviceable, @delivery_days, @created_by_email)";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@pincode", model.Pincode);
            cmd.Parameters.AddWithValue("@city", model.City ?? "");
            cmd.Parameters.AddWithValue("@state", model.State ?? "");
            cmd.Parameters.AddWithValue("@is_serviceable", model.IsServiceable);
            cmd.Parameters.AddWithValue("@delivery_days", model.DeliveryDays);
            cmd.Parameters.AddWithValue("@created_by_email", userEmail);

            await cmd.ExecuteNonQueryAsync();

            return new { Success = true, Message = "Pincode added successfully" };
        }

        // ✅ LIST
        public async Task<object> GetAllPinCodes()
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            var list = new List<object>();

            string query = "SELECT * FROM delivery_pincodes ORDER BY created_at DESC";

            using var cmd = new NpgsqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    Id = reader.GetGuid(0),
                    Pincode = reader.GetString(1),
                    City = reader.GetString(2),
                    State = reader.GetString(3),
                    IsServiceable = reader.GetBoolean(4),
                    DeliveryDays = reader.GetInt32(5),
                    CreatedBy = reader.GetString(6)
                });
            }

            return list;
        }

        // ✅ CHECK
        public async Task<object> CheckPincode(string pincode)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string query = "SELECT is_serviceable, delivery_days FROM delivery_pincodes WHERE pincode=@pincode";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@pincode", pincode);

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new
                {
                    Available = reader.GetBoolean(0),
                    DeliveryDays = reader.GetInt32(1)
                };
            }

            return new { Available = false, Message = "Not serviceable" };
        }

        // ✅ UPDATE
        public async Task<object> UpdatePinCode(Guid id, string userEmail, AddPincodeRequest model)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string query = @"UPDATE delivery_pincodes SET 
            pincode=@pincode, city=@city, state=@state,
            is_serviceable=@is_serviceable, delivery_days=@delivery_days
            WHERE id=@id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@pincode", model.Pincode);
            cmd.Parameters.AddWithValue("@city", model.City ?? "");
            cmd.Parameters.AddWithValue("@state", model.State ?? "");
            cmd.Parameters.AddWithValue("@is_serviceable", model.IsServiceable);
            cmd.Parameters.AddWithValue("@delivery_days", model.DeliveryDays);

            await cmd.ExecuteNonQueryAsync();

            return new { Success = true, Message = "Updated successfully" };
        }

        // ✅ DELETE
        public async Task<object> DeletePinCode(Guid id)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string query = "DELETE FROM delivery_pincodes WHERE id=@id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await cmd.ExecuteNonQueryAsync();

            return new { Success = true, Message = "Deleted successfully" };
        }
    }
}
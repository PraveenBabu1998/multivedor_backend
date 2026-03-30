using elemechWisetrack.Models;
using Npgsql;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLayer_Coupon
    {
        Task<object> CreateCoupon(string email, CouponCodeCreate request);
        Task<object> GetCoupons();
        Task<object> GetCouponById(Guid id);
        Task<object> UpdateCoupon(Guid id, CouponCodeCreate request);
        Task<object> DeleteCoupon(Guid id);
        Task<object> ApplyCoupon(string email, ApplyCouponRequest request);
        Task<object> GetCouponUsage();
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Coupon { }

    public partial class DataBaseLayer
    {
        public async Task<object> CreateCoupon(string email, CouponCodeCreate request)
        {
            try
            {
                using var conn = new NpgsqlConnection(DbConnection);
                await conn.OpenAsync();

                var query = @"INSERT INTO coupons
        (id, code, description, discount_type, discount_value,
         min_order_amount, max_discount_amount, usage_limit,
         start_date, end_date, applicable_on, created_by)
        VALUES
        (@id, @code, @desc, @type, @value,
         @min, @max, @limit,
         @start, @end, @applicable, @created_by)";

                using var cmd = new NpgsqlCommand(query, conn);

                var couponId = Guid.NewGuid();

                cmd.Parameters.AddWithValue("@id", couponId);
                cmd.Parameters.AddWithValue("@code", request.Code);
                cmd.Parameters.AddWithValue("@desc", request.Description ?? "");
                cmd.Parameters.AddWithValue("@type", request.DiscountType);
                cmd.Parameters.AddWithValue("@value", request.DiscountValue);
                cmd.Parameters.AddWithValue("@min", request.MinOrderAmount);
                cmd.Parameters.AddWithValue("@max", (object?)request.MaxDiscountAmount ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@limit", (object?)request.UsageLimit ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@start", request.StartDate);
                cmd.Parameters.AddWithValue("@end", request.EndDate);
                cmd.Parameters.AddWithValue("@applicable", request.ApplicableOn);
                cmd.Parameters.AddWithValue("@created_by", email);

                await cmd.ExecuteNonQueryAsync();

                // Insert product mapping
                if (request.ProductIds != null && request.ProductIds.Any())
                {
                    foreach (var productId in request.ProductIds)
                    {
                        var mapQuery = @"INSERT INTO coupon_products(id, coupon_id, product_id)
                                 VALUES(@id, @coupon_id, @product_id)";

                        using var mapCmd = new NpgsqlCommand(mapQuery, conn);
                        mapCmd.Parameters.AddWithValue("@id", Guid.NewGuid());
                        mapCmd.Parameters.AddWithValue("@coupon_id", couponId);
                        mapCmd.Parameters.AddWithValue("@product_id", productId);

                        await mapCmd.ExecuteNonQueryAsync();
                    }
                }

                return new { success = true, message = "Coupon created" };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

        public async Task<object> GetCoupons()
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            var list = new List<object>();

            var cmd = new NpgsqlCommand("SELECT * FROM coupons", conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    id = reader["id"],
                    code = reader["code"],
                    type = reader["discount_type"],
                    value = reader["discount_value"]
                });
            }

            return list;
        }

        public async Task<object> GetCouponById(Guid id)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            var query = "SELECT * FROM coupons WHERE id = @id";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new
                {
                    id = reader["id"],
                    code = reader["code"],
                    description = reader["description"],
                    discount_type = reader["discount_type"],
                    discount_value = reader["discount_value"],
                    min_order_amount = reader["min_order_amount"],
                    max_discount_amount = reader["max_discount_amount"],
                    usage_limit = reader["usage_limit"],
                    start_date = reader["start_date"],
                    end_date = reader["end_date"],
                    applicable_on = reader["applicable_on"]
                };
            }

            return new { success = false, message = "Coupon not found" };
        }

        public async Task<object> UpdateCoupon(Guid id, CouponCodeCreate request)
        {
            try
            {
                using var conn = new NpgsqlConnection(DbConnection);
                await conn.OpenAsync();

                var query = @"UPDATE coupons SET
                        code = @code,
                        description = @desc,
                        discount_type = @type,
                        discount_value = @value,
                        min_order_amount = @min,
                        max_discount_amount = @max,
                        usage_limit = @limit,
                        start_date = @start,
                        end_date = @end,
                        applicable_on = @applicable
                      WHERE id = @id";

                using var cmd = new NpgsqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@code", request.Code);
                cmd.Parameters.AddWithValue("@desc", request.Description ?? "");
                cmd.Parameters.AddWithValue("@type", request.DiscountType);
                cmd.Parameters.AddWithValue("@value", request.DiscountValue);
                cmd.Parameters.AddWithValue("@min", request.MinOrderAmount);
                cmd.Parameters.AddWithValue("@max", (object?)request.MaxDiscountAmount ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@limit", (object?)request.UsageLimit ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@start", request.StartDate);
                cmd.Parameters.AddWithValue("@end", request.EndDate);
                cmd.Parameters.AddWithValue("@applicable", request.ApplicableOn);

                var rows = await cmd.ExecuteNonQueryAsync();

                if (rows == 0)
                    return new { success = false, message = "Coupon not found" };

                return new { success = true, message = "Coupon updated successfully" };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

        public async Task<object> DeleteCoupon(Guid id)
        {
            try
            {
                using var conn = new NpgsqlConnection(DbConnection);
                await conn.OpenAsync();

                var query = "DELETE FROM coupons WHERE id = @id";
                using var cmd = new NpgsqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@id", id);

                var rows = await cmd.ExecuteNonQueryAsync();

                if (rows == 0)
                    return new { success = false, message = "Coupon not found" };

                return new { success = true, message = "Coupon deleted successfully" };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

        public async Task<object> ApplyCoupon(string email, ApplyCouponRequest request)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            // STEP 1: Get Coupon
            var cmd = new NpgsqlCommand("SELECT * FROM coupons WHERE code=@code", conn);
            cmd.Parameters.AddWithValue("@code", request.CouponCode);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!reader.Read())
                return new { success = false, message = "Invalid coupon" };

            var couponId = Guid.Parse(reader["id"].ToString());
            var discountType = reader["discount_type"].ToString();
            var discountValue = Convert.ToDecimal(reader["discount_value"]);
            var minAmount = Convert.ToDecimal(reader["min_order_amount"]);

            if (request.CartAmount < minAmount)
                return new { success = false, message = "Minimum amount not met" };

            decimal discount = 0;

            if (discountType == "percentage")
                discount = (request.CartAmount * discountValue) / 100;
            else
                discount = discountValue;

            var finalAmount = request.CartAmount - discount;

            // ⚠️ IMPORTANT: Close reader before new query
            await reader.CloseAsync();

            // STEP 2: Save Coupon Usage
            if (request.ProductIds != null && request.ProductIds.Any())
            {
                foreach (var productId in request.ProductIds)
                {
                    var insertQuery = @"INSERT INTO coupon_usage
                        (id, coupon_id, user_email, product_id)
                        VALUES (@id, @coupon_id, @user_email, @product_id)";

                    using var insertCmd = new NpgsqlCommand(insertQuery, conn);

                    insertCmd.Parameters.AddWithValue("@id", Guid.NewGuid());
                    insertCmd.Parameters.AddWithValue("@coupon_id", couponId);
                    insertCmd.Parameters.AddWithValue("@user_email", email); // ✅ email here
                    insertCmd.Parameters.AddWithValue("@product_id", productId);

                    await insertCmd.ExecuteNonQueryAsync();
                }
            }

            return new
            {
                success = true,
                discount,
                finalAmount
            };
        }

        public async Task<object> GetCouponUsage()
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            var list = new List<object>();

            var query = @"SELECT 
                    cu.id,
                    cu.user_email,
                    cu.product_id,
                    p.name AS product_name,
                    cu.used_at,
                    c.code AS coupon_code
                  FROM coupon_usage cu
                  JOIN coupons c ON cu.coupon_id = c.id
                  JOIN products p ON cu.product_id = p.id
                  ORDER BY cu.used_at DESC";

            using var cmd = new NpgsqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    id = reader["id"],
                    couponCode = reader["coupon_code"],
                    userEmail = reader["user_email"],
                    productId = reader["product_id"],
                    productName = reader["product_name"], // ✅ added
                    usedAt = reader["used_at"]
                });
            }

            return list;
        }
    }
}

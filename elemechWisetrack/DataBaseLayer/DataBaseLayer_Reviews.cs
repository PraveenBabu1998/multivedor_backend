using elemechWisetrack.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLayer_Reviews
    {
        Task<object> AddReviews(string productId, string email, ReviewModel request);
        Task<object> GetReviewsByProduct(string productId);
        Task<object> UpdateReview(string reviewId, string email, ReviewModel request);
        Task<object> DeleteReview(string reviewId, string email);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Reviews { }

    public partial class DataBaseLayer
    {
        // ✅ ADD REVIEW
        public async Task<object> AddReviews(string productId, string email, ReviewModel request)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            var query = @"
        INSERT INTO reviews (product_id, email, rating, comment)
        VALUES (@ProductId, @Email, @Rating, @Comment)
        RETURNING id;
        ";

            using var cmd = new NpgsqlCommand(query, con);
            cmd.Parameters.AddWithValue("@ProductId", Guid.Parse(productId));
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@Rating", request.Rating);
            cmd.Parameters.AddWithValue("@Comment", request.Comment ?? "");

            var reviewId = await cmd.ExecuteScalarAsync();

            return new { success = true, reviewId };
        }

        // ✅ GET REVIEWS
        public async Task<object> GetReviewsByProduct(string productId)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            var query = @"
        SELECT id, product_id, email, rating, comment, created_at
        FROM reviews
        WHERE product_id = @ProductId
        ORDER BY created_at DESC;
        ";

            using var cmd = new NpgsqlCommand(query, con);
            cmd.Parameters.AddWithValue("@ProductId", Guid.Parse(productId));

            var reader = await cmd.ExecuteReaderAsync();

            var list = new List<object>();

            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    id = reader["id"],
                    productId = reader["product_id"],
                    email = reader["email"],
                    rating = reader["rating"],
                    comment = reader["comment"],
                    createdAt = reader["created_at"]
                });
            }

            return list;
        }

        // ✅ UPDATE REVIEW (only owner)
        public async Task<object> UpdateReview(string reviewId, string email, ReviewModel request)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            var query = @"
        UPDATE reviews
        SET rating = @Rating,
            comment = @Comment,
            updated_at = NOW()
        WHERE id = @ReviewId AND email = @Email;
        ";

            using var cmd = new NpgsqlCommand(query, con);
            cmd.Parameters.AddWithValue("@ReviewId", Guid.Parse(reviewId));
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@Rating", request.Rating);
            cmd.Parameters.AddWithValue("@Comment", request.Comment ?? "");

            var rows = await cmd.ExecuteNonQueryAsync();

            return new
            {
                success = rows > 0,
                message = rows > 0 ? "Updated successfully" : "Not authorized or not found"
            };
        }

        // ✅ DELETE REVIEW (only owner)
        public async Task<object> DeleteReview(string reviewId, string email)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            var query = @"DELETE FROM reviews WHERE id = @ReviewId AND email = @Email;";

            using var cmd = new NpgsqlCommand(query, con);
            cmd.Parameters.AddWithValue("@ReviewId", Guid.Parse(reviewId));
            cmd.Parameters.AddWithValue("@Email", email);

            var rows = await cmd.ExecuteNonQueryAsync();

            return new
            {
                success = rows > 0,
                message = rows > 0 ? "Deleted successfully" : "Not authorized or not found"
            };
        }
    }
}

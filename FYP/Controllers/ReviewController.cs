using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;
using FYP.Models;

[Route("api/[controller]")]
[ApiController]
public class ReviewController : ControllerBase
{
    private readonly string _connectionString;

    public ReviewController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    // GET: api/Review
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Review>>> GetReviews()
    {
        var reviews = new List<Review>();

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var command = new SqlCommand("SELECT * FROM Reviews", connection);
            var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var review = new Review
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    RestaurantId = reader.GetInt32(reader.GetOrdinal("RestaurantId")),
                    Rating = reader.GetInt32(reader.GetOrdinal("Rating")),
                    Comment = reader.IsDBNull(reader.GetOrdinal("Comment")) ? null : reader.GetString(reader.GetOrdinal("Comment")),
                    DateAdded = reader.GetDateTime(reader.GetOrdinal("DateAdded"))
                };
                reviews.Add(review);
            }
        }

        return Ok(reviews);
    }

    // GET: api/Review/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Review>> GetReview(int id)
    {
        Review review = null;

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var command = new SqlCommand("SELECT * FROM Reviews WHERE Id = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);

            var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                review = new Review
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    RestaurantId = reader.GetInt32(reader.GetOrdinal("RestaurantId")),
                    Rating = reader.GetInt32(reader.GetOrdinal("Rating")),
                    Comment = reader.IsDBNull(reader.GetOrdinal("Comment")) ? null : reader.GetString(reader.GetOrdinal("Comment")),
                    DateAdded = reader.GetDateTime(reader.GetOrdinal("DateAdded"))
                };
            }
        }

        if (review == null)
        {
            return NotFound();
        }

        return Ok(review);
    }

    // POST: api/Review
    [HttpPost]
    public async Task<ActionResult<Review>> PostReview(Review review)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var command = new SqlCommand("INSERT INTO Reviews (RestaurantId, Rating, Comment) OUTPUT INSERTED.Id VALUES (@RestaurantId, @Rating, @Comment)", connection);
            command.Parameters.AddWithValue("@RestaurantId", review.RestaurantId);
            command.Parameters.AddWithValue("@Rating", review.Rating);
            command.Parameters.AddWithValue("@Comment", (object)review.Comment ?? DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            review.Id = Convert.ToInt32(result);
        }

        return CreatedAtAction(nameof(GetReview), new { id = review.Id }, review);
    }

    // PUT: api/Review/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutReview(int id, Review review)
    {
        if (id != review.Id)
        {
            return BadRequest();
        }

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var command = new SqlCommand("UPDATE Reviews SET Rating = @Rating, Comment = @Comment WHERE Id = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Rating", review.Rating);
            command.Parameters.AddWithValue("@Comment", (object)review.Comment ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        return NoContent();
    }

    // DELETE: api/Review/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReview(int id)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var command = new SqlCommand("DELETE FROM Reviews WHERE Id = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
            {
                return NotFound();
            }
        }

        return NoContent();
    }
}

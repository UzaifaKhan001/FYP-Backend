using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;
using FYP.Models;

[Route("api/[controller]")]
[ApiController]
public class RestaurantController : ControllerBase
{
    private readonly string _connectionString;

    public RestaurantController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    // GET: api/Restaurant
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Restaurant>>> GetRestaurants()
    {
        var restaurants = new List<Restaurant>();

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var command = new SqlCommand("SELECT * FROM Restaurants", connection);
            var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var restaurant = new Restaurant
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString(reader.GetOrdinal("Address")),
                    Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone"))
                };
                restaurants.Add(restaurant);
            }
        }

        return Ok(restaurants);
    }

    // GET: api/Restaurant/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Restaurant>> GetRestaurant(int id)
    {
        Restaurant restaurant = null;

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var command = new SqlCommand("SELECT * FROM Restaurants WHERE Id = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);

            var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                restaurant = new Restaurant
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString(reader.GetOrdinal("Address")),
                    Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone"))
                };
            }
        }

        if (restaurant == null)
        {
            return NotFound();
        }

        return Ok(restaurant);
    }

    // POST: api/Restaurant
    [HttpPost]
    public async Task<ActionResult<Restaurant>> PostRestaurant(Restaurant restaurant)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var command = new SqlCommand("INSERT INTO Restaurants (Name, Address, Phone) OUTPUT INSERTED.Id VALUES (@Name, @Address, @Phone)", connection);
            command.Parameters.AddWithValue("@Name", restaurant.Name);
            command.Parameters.AddWithValue("@Address", (object)restaurant.Address ?? DBNull.Value);
            command.Parameters.AddWithValue("@Phone", (object)restaurant.Phone ?? DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            restaurant.Id = Convert.ToInt32(result);
        }

        return CreatedAtAction(nameof(GetRestaurant), new { id = restaurant.Id }, restaurant);
    }

    // PUT: api/Restaurant/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutRestaurant(int id, Restaurant restaurant)
    {
        if (id != restaurant.Id)
        {
            return BadRequest();
        }

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var command = new SqlCommand("UPDATE Restaurants SET Name = @Name, Address = @Address, Phone = @Phone WHERE Id = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Name", restaurant.Name);
            command.Parameters.AddWithValue("@Address", (object)restaurant.Address ?? DBNull.Value);
            command.Parameters.AddWithValue("@Phone", (object)restaurant.Phone ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        return NoContent();
    }

    // DELETE: api/Restaurant/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRestaurant(int id)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var command = new SqlCommand("DELETE FROM Restaurants WHERE Id = @Id", connection);
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

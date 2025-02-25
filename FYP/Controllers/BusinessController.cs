using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;
using FYP.Models;
using Microsoft.Extensions.Configuration;

[Route("api/[controller]")]
[ApiController]
public class BusinessController : ControllerBase
{
    private readonly string _connectionString;

    public BusinessController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    // GET: api/business/getBusinessTypes
    // POST: api/business/addBusinessType
    [HttpPost("addBusinessType")]
    public async Task<ActionResult<BusinessType>> AddBusinessType([FromBody] BusinessType businessType)
    {
        if (businessType == null || string.IsNullOrWhiteSpace(businessType.Name))
        {
            return BadRequest("Invalid business type data.");
        }

        string query = @"INSERT INTO BusinessTypes (Name) OUTPUT INSERTED.Id VALUES (@Name)";

        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Name", businessType.Name);

                var result = await command.ExecuteScalarAsync();
                businessType.Id = Convert.ToInt32(result);
            }

            return CreatedAtAction(nameof(GetBusinessTypes), new { id = businessType.Id }, businessType);
        }
        catch (SqlException ex)
        {
            return StatusCode(500, new { message = "Error adding business type to the database.", error = ex.Message });
        }
    }

    [HttpGet("getBusinessTypes")]
    public async Task<ActionResult<IEnumerable<BusinessType>>> GetBusinessTypes()
    {
        var businessTypes = new List<BusinessType>();
        string query = "SELECT Id, Name FROM BusinessTypes";

        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(query, connection);
                var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var businessType = new BusinessType
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Name = reader.GetString(reader.GetOrdinal("Name"))
                    };
                    businessTypes.Add(businessType);
                }
            }

            return Ok(businessTypes);
        }
        catch (SqlException ex)
        {
            return StatusCode(500, new { message = "Error fetching business types from the database.", error = ex.Message });
        }
    }

    // POST: api/business/validateAddress
    [HttpPost("validateAddress")]
    public IActionResult ValidateAddress([FromBody] ValidateAddressRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Address) || request.Address.Length < 10)
        {
            return BadRequest("Invalid address.");
        }

        return Ok("Address is valid.");
    }

    public class ValidateAddressRequest
    {
        public string Address { get; set; }
    }
    // POST: api/business/addBusiness
    [HttpPost("addBusiness")]
    public async Task<ActionResult<Business>> AddBusiness([FromBody] Business business)
    {
        if (business == null)
        {
            return BadRequest("Invalid business data.");
        }

        // Check if all required fields are filled
        if (string.IsNullOrWhiteSpace(business.StoreAddress) ||
            string.IsNullOrWhiteSpace(business.StoreName) ||
            string.IsNullOrWhiteSpace(business.BrandName) ||
            string.IsNullOrWhiteSpace(business.FirstName) ||
            string.IsNullOrWhiteSpace(business.LastName) ||
            string.IsNullOrWhiteSpace(business.Email) ||
            string.IsNullOrWhiteSpace(business.PhoneNumber))
        {
            return BadRequest("All fields are required.");
        }

        // Validate that the BusinessTypeId exists in the BusinessTypes table
        string validateBusinessTypeQuery = "SELECT COUNT(*) FROM BusinessTypes WHERE Id = @BusinessTypeId";
        bool isValidBusinessType = false;

        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(validateBusinessTypeQuery, connection);
                command.Parameters.AddWithValue("@BusinessTypeId", business.BusinessTypeId);
                var count = (int)await command.ExecuteScalarAsync();
                isValidBusinessType = count > 0;
            }

            if (!isValidBusinessType)
            {
                return BadRequest("Invalid BusinessTypeId.");
            }

            // Proceed to insert the new business record
            string query = @"INSERT INTO Businesses 
                         (StoreAddress, FloorSuite, StoreName, BrandName, BusinessTypeId, FirstName, LastName, Email, PhoneNumber, AgreedToPrivacy) 
                         OUTPUT INSERTED.Id
                         VALUES 
                         (@StoreAddress, @FloorSuite, @StoreName, @BrandName, @BusinessTypeId, @FirstName, @LastName, @Email, @PhoneNumber, @AgreedToPrivacy)";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@StoreAddress", business.StoreAddress);
                command.Parameters.AddWithValue("@FloorSuite", string.IsNullOrEmpty(business.FloorSuite) ? (object)DBNull.Value : business.FloorSuite);
                command.Parameters.AddWithValue("@StoreName", business.StoreName);
                command.Parameters.AddWithValue("@BrandName", business.BrandName);
                command.Parameters.AddWithValue("@BusinessTypeId", business.BusinessTypeId);
                command.Parameters.AddWithValue("@FirstName", business.FirstName);
                command.Parameters.AddWithValue("@LastName", business.LastName);
                command.Parameters.AddWithValue("@Email", business.Email);
                command.Parameters.AddWithValue("@PhoneNumber", business.PhoneNumber);
                command.Parameters.AddWithValue("@AgreedToPrivacy", business.AgreedToPrivacy);

                var result = await command.ExecuteScalarAsync();
                business.Id = Convert.ToInt32(result);  // Set the generated Id value
            }

            return CreatedAtAction(nameof(GetBusinessTypes), new { id = business.Id }, business);
        }
        catch (SqlException ex)
        {
            return StatusCode(500, new { message = "Error adding business to the database.", error = ex.Message });
        }
    }
}


using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace FYP.Repository
{
    public class RestaurantRepository
    {
        private readonly string _connectionString;

        public RestaurantRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> AddRestaurantAsync(string name, string picturePath)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = "INSERT INTO Restaurants (Name, Picture) VALUES (@Name, @Picture); SELECT SCOPE_IDENTITY();";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Name", name);
                        command.Parameters.AddWithValue("@Picture", picturePath);

                        var result = await command.ExecuteScalarAsync();
                        if (result != DBNull.Value)
                        {
                            return Convert.ToInt32(result);
                        }
                        else
                        {
                            throw new Exception("Failed to retrieve the inserted restaurant ID.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log exception and rethrow
                throw new Exception("An error occurred while adding the restaurant.", ex);
            }
        }

        public async Task<DataTable> GetAllRestaurantsAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT * FROM Restaurants";

                    using (var command = new SqlCommand(query, connection))
                    using (var adapter = new SqlDataAdapter(command))
                    {
                        var dataTable = new DataTable();
                        // Run Fill operation asynchronously
                        await Task.Run(() => adapter.Fill(dataTable));
                        return dataTable;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log exception and rethrow
                throw new Exception("An error occurred while fetching all restaurants.", ex);
            }
        }
    }
}

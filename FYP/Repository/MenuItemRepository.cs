using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace FYP.Repository
{
    public class Repository
    {
        private readonly string _connectionString;

        public Repository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> AddMenuItemAsync(string name, string picturePath, decimal price, int restaurantId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = "INSERT INTO MenuItems (Name, Picture, Price, RestaurantId) " +
                                   "VALUES (@Name, @Picture, @Price, @RestaurantId); " +
                                   "SELECT SCOPE_IDENTITY();";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Name", name);
                        command.Parameters.AddWithValue("@Picture", picturePath);
                        command.Parameters.AddWithValue("@Price", price);
                        command.Parameters.AddWithValue("@RestaurantId", restaurantId);

                        var result = await command.ExecuteScalarAsync();
                        if (result != DBNull.Value)
                        {
                            return Convert.ToInt32(result);
                        }
                        else
                        {
                            throw new Exception("Failed to retrieve the inserted menu item ID.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while adding the menu item.", ex);
            }
        }

        public async Task<DataTable> GetMenuItemsByRestaurantIdAsync(int restaurantId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT * FROM MenuItems WHERE RestaurantId = @RestaurantId";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RestaurantId", restaurantId);

                        using (var adapter = new SqlDataAdapter(command))
                        {
                            var dataTable = new DataTable();
                            await Task.Run(() => adapter.Fill(dataTable));
                            return dataTable;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while fetching the menu items.", ex);
            }
        }
    }
}

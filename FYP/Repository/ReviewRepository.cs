using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace FYP.Repository
{
    public class ReviewRepository
    {
        private readonly string _connectionString;

        public ReviewRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> AddReviewAsync(string text, int rating, string waiterName, string tableNumber, decimal orderPrice, int restaurantId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = "INSERT INTO Reviews (Text, Rating, WaiterName, TableNumber, OrderPrice, RestaurantId) " +
                                   "VALUES (@Text, @Rating, @WaiterName, @TableNumber, @OrderPrice, @RestaurantId); " +
                                   "SELECT SCOPE_IDENTITY();";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Text", text);
                        command.Parameters.AddWithValue("@Rating", rating);
                        command.Parameters.AddWithValue("@WaiterName", waiterName);
                        command.Parameters.AddWithValue("@TableNumber", tableNumber);
                        command.Parameters.AddWithValue("@OrderPrice", orderPrice);
                        command.Parameters.AddWithValue("@RestaurantId", restaurantId);

                        var result = await command.ExecuteScalarAsync();
                        if (result != DBNull.Value)
                        {
                            return Convert.ToInt32(result);
                        }
                        else
                        {
                            throw new Exception("Failed to retrieve the inserted review ID.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while adding the review.", ex);
            }
        }

        public async Task<DataTable> GetReviewsByRestaurantIdAsync(int restaurantId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT * FROM Reviews WHERE RestaurantId = @RestaurantId";

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
                throw new Exception("An error occurred while fetching reviews.", ex);
            }
        }
    }
}

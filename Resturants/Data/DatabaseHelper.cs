using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Restaurants.Models;

namespace Restaurants.Data
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public async Task<bool> RegisterUserAsync(User user, string password)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string checkQuery = "SELECT COUNT(*) FROM ResUsers WHERE Email = @Email";
                using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@Email", user.Email);
                    int count = (int)await checkCmd.ExecuteScalarAsync();
                    if (count > 0) return false;
                }

                string insertQuery = "INSERT INTO ResUsers (Username, Email, PasswordHash) VALUES (@Username, @Email, @Password)";
                using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", user.Username);
                    cmd.Parameters.AddWithValue("@Email", user.Email);
                    cmd.Parameters.AddWithValue("@Password", BCrypt.Net.BCrypt.HashPassword(password));
                    await cmd.ExecuteNonQueryAsync();
                    return true;
                }
            }
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT Id, Username, Email, PasswordHash FROM ResUsers WHERE Email = @Email";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new User
                            {
                                Id = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Email = reader.GetString(2),
                                PasswordHash = reader.GetString(3)
                            };
                        }
                    }
                }
            }
            return null;
        }

        public async Task<bool> UpdatePasswordAsync(string email, string newPassword)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = "UPDATE ResUsers SET PasswordHash = @Password WHERE Email = @Email";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Password", BCrypt.Net.BCrypt.HashPassword(newPassword));
                    cmd.Parameters.AddWithValue("@Email", email);
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }

        public async Task<bool> DeleteUserAsync(string email)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = "DELETE FROM ResUsers WHERE Email = @Email";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }
    }
}

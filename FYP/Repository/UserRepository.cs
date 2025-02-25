using FYP.Models;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace FYP.Repository
{
    public class UserRepository
    {
        private readonly string _connectionString;

        public UserRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

       
        // Get user by Id
        public async Task<User> GetUserByIdAsync(int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var command = new SqlCommand("SELECT Id, Name, Email, Password FROM Users WHERE Id = @UserId", connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new User
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Name = reader["Name"].ToString(),
                            Email = reader["Email"].ToString(),
                            PasswordHash = reader["Password"].ToString()
                        };
                    }
                }

                return null; // User not found
            }
        }

        // Update user's password
        public async Task<bool> UpdatePasswordAsync(int userId, string PasswordHash, string newPassword)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var command = new SqlCommand("SELECT PasswordHash  FROM Users WHERE Id = @UserId", connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var storedPassword = reader["PasswordHash "].ToString();
                        if (storedPassword != PasswordHash ) // Ensure password comparison is secure
                        {
                            return false;
                        }
                    }
                }

                // Update password if old password is correct
                var updateCommand = new SqlCommand("UPDATE Users SET Password = @NewPassword WHERE Id = @UserId", connection);
                updateCommand.Parameters.AddWithValue("@NewPassword", newPassword); // Make sure new password is hashed
                updateCommand.Parameters.AddWithValue("@UserId", userId);

                var rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
        }

        // Update user settings
        public async Task<bool> UpdateUserSettingsAsync(UserSettings settings)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var command = new SqlCommand("UPDATE UserSettings SET EmailNotifications = @EmailNotifications, PushNotifications = @PushNotifications, Updates = @Updates, ProfileVisibility = @ProfileVisibility, ActivityStatus = @ActivityStatus, EnableSound = @EnableSound, Volume = @Volume WHERE UserId = @UserId", connection);
                command.Parameters.AddWithValue("@EmailNotifications", settings.EmailNotifications);
                command.Parameters.AddWithValue("@PushNotifications", settings.PushNotifications);
                command.Parameters.AddWithValue("@Updates", settings.Updates);
                command.Parameters.AddWithValue("@ProfileVisibility", settings.ProfileVisibility);
                command.Parameters.AddWithValue("@ActivityStatus", settings.ActivityStatus);
                command.Parameters.AddWithValue("@EnableSound", settings.EnableSound);
                command.Parameters.AddWithValue("@Volume", settings.Volume);
                command.Parameters.AddWithValue("@UserId", settings.UserId);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
        }

        // Get user profile
        public async Task<UserProfile> GetUserProfileAsync(int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var command = new SqlCommand("SELECT * FROM UserProfiles WHERE UserId = @UserId", connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new UserProfile
                        {
                            UserId = userId,
                            // Map other fields from the UserProfiles table
                        };
                    }
                }

                return null;
            }
        }
    }
}

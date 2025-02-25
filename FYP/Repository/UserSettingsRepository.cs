using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;  // Include this namespace for async/await
using FYP.Models;

namespace FYP.Services
{
    public class UserSettingsRepository
    {
        private readonly string _connectionString;

        public UserSettingsRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<bool> InsertUserSettingsAsync(UserSettings settings)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    const string query = @"
                        INSERT INTO UserSettings (UserId, EmailNotifications, PushNotifications, Updates, ProfileVisibility, ActivityStatus, EnableSound, Volume)
                        VALUES (@UserId, @EmailNotifications, @PushNotifications, @Updates, @ProfileVisibility, @ActivityStatus, @EnableSound, @Volume)";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", settings.UserId);
                        command.Parameters.AddWithValue("@EmailNotifications", settings.EmailNotifications);
                        command.Parameters.AddWithValue("@PushNotifications", settings.PushNotifications);
                        command.Parameters.AddWithValue("@Updates", settings.Updates);
                        command.Parameters.AddWithValue("@ProfileVisibility", settings.ProfileVisibility);
                        command.Parameters.AddWithValue("@ActivityStatus", settings.ActivityStatus);
                        command.Parameters.AddWithValue("@EnableSound", settings.EnableSound);
                        command.Parameters.AddWithValue("@Volume", settings.Volume);

                        connection.Open();
                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in InsertUserSettingsAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateUserSettingsAsync(UserSettings settings)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    const string query = @"
                        UPDATE UserSettings 
                        SET 
                            EmailNotifications = @EmailNotifications,
                            PushNotifications = @PushNotifications,
                            Updates = @Updates,
                            ProfileVisibility = @ProfileVisibility,
                            ActivityStatus = @ActivityStatus,
                            EnableSound = @EnableSound,
                            Volume = @Volume
                        WHERE UserId = @UserId";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", settings.UserId);
                        command.Parameters.AddWithValue("@EmailNotifications", settings.EmailNotifications);
                        command.Parameters.AddWithValue("@PushNotifications", settings.PushNotifications);
                        command.Parameters.AddWithValue("@Updates", settings.Updates);
                        command.Parameters.AddWithValue("@ProfileVisibility", settings.ProfileVisibility);
                        command.Parameters.AddWithValue("@ActivityStatus", settings.ActivityStatus);
                        command.Parameters.AddWithValue("@EnableSound", settings.EnableSound);
                        command.Parameters.AddWithValue("@Volume", settings.Volume);

                        connection.Open();
                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateUserSettingsAsync: {ex.Message}");
                return false;
            }
        }
    }
}

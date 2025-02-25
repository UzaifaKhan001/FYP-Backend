using FYP.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace FYP.Repository
{
    public class NotificationRepository
    {
        private readonly string _connectionString;

        public NotificationRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<Notification>> GetNotificationsByUserIdAsync(int userId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string query = "SELECT * FROM Notifications WHERE UserId = @UserId ORDER BY CreatedAt DESC";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);

                        connection.Open();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var notifications = new List<Notification>();
                            while (await reader.ReadAsync())
                            {
                                notifications.Add(new Notification
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                                    Message = reader.GetString(reader.GetOrdinal("Message")),
                                    Read = reader.GetBoolean(reader.GetOrdinal("Read")),
                                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                                });
                            }
                            return notifications;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching notifications from the database", ex);
            }
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string query = "UPDATE Notifications SET [Read] = 1 WHERE Id = @Id";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", notificationId);

                        connection.Open();
                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error marking notification as read in the database", ex);
            }
        }

        public async Task CreateNotificationAsync(Notification notification)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string query = "INSERT INTO Notifications (UserId, Message, [Read], CreatedAt) VALUES (@UserId, @Message, @Read, @CreatedAt)";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", notification.UserId);
                        command.Parameters.AddWithValue("@Message", notification.Message);
                        command.Parameters.AddWithValue("@Read", notification.Read);
                        command.Parameters.AddWithValue("@CreatedAt", notification.CreatedAt);

                        connection.Open();
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating notification in the database", ex);
            }
        }
    }
}

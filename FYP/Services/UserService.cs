namespace FYP.Services
{
    using FYP.Models;
    using FYP.Repository;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.Threading.Tasks;

    public class UserService
    {
        private readonly string _connectionString;
        private readonly NotificationRepository _notificationRepository;
        private readonly UserRepository _UserRepository;
        private readonly UserSettingsRepository _UserSettingsRepository;
        private readonly IPasswordHasher<User> _passwordHasher;


        public UserService(
            IConfiguration configuration, // Inject IConfiguration
            NotificationRepository notificationRepository,
            UserRepository userRepository,
            UserSettingsRepository userSettingsRepository,
            IPasswordHasher<User> passwordHasher)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? throw new ArgumentNullException("Connection string is missing.");

            _notificationRepository = notificationRepository;
            _UserRepository = userRepository;
            _UserSettingsRepository = userSettingsRepository;
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        }

        // Authenticate User
        public async Task<User> AuthenticateAsync(string email, string password)
        {
            const string query = "SELECT Id, Name, Email, PasswordHash, CreatedAt, ResetToken, ResetTokenExpiration FROM Users WHERE Email = @Email";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var user = new User
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Email = reader.GetString(2),
                                PasswordHash = reader.GetString(3),
                                CreatedAt = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4),
                                ResetToken = reader.IsDBNull(5) ? null : reader.GetString(5),
                                ResetTokenExpiration = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6)
                            };

                            if (!VerifyPassword(password, user.PasswordHash))
                            {
                                throw new UnauthorizedAccessException("Invalid credentials");
                            }

                            return user;
                        }
                    }
                }
            }

            throw new UnauthorizedAccessException("Invalid credentials");
        }

        // Register User
        public async Task<User> RegisterAsync(string name, string email, string password)
        {
            var existingUser = await GetUserByEmailAsync(email);
            if (existingUser != null)
            {
                throw new Exception("Email is already in use.");
            }

            // Use bcrypt to securely hash the password
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

            // Ensure hashed password is valid (should be at least 60 chars, starts with "$2")
            if (string.IsNullOrWhiteSpace(hashedPassword) || !hashedPassword.StartsWith("$2"))
            {
                throw new Exception("Failed to generate a valid password hash.");
            }

            const string insertUserQuery = @"
    INSERT INTO Users (Name, Email, PasswordHash, CreatedAt) 
    OUTPUT INSERTED.Id 
    VALUES (@Name, @Email, @PasswordHash, @CreatedAt)";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(insertUserQuery, connection))
                {
                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                    command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

                    var userId = (int)await command.ExecuteScalarAsync();

                    // Send notification after successful registration
                    await InsertNotificationAsync(connection, userId, "Welcome! Your account has been created successfully.");

                    return new User
                    {
                        Id = userId,
                        Name = name,
                        Email = email,
                        PasswordHash = hashedPassword,
                        CreatedAt = DateTime.UtcNow
                    };
                }
            }
        }

        private async Task InsertNotificationAsync(SqlConnection connection, int userId, string message)
        {
            const string insertNotificationQuery = @"
    INSERT INTO Notifications (UserId, Message, [Read], CreatedAt)
    VALUES (@UserId, @Message, @Read, @CreatedAt)";

            using (var command = new SqlCommand(insertNotificationQuery, connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@Message", message);
                command.Parameters.AddWithValue("@Read", false); // Ensure this is boolean (bit) in SQL
                command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

                await command.ExecuteNonQueryAsync();
            }
        }
        // Get User by Email
        public async Task<User> GetUserByEmailAsync(string email)
        {
            const string query = "SELECT Id, Name, Email, PasswordHash, CreatedAt, ResetToken, ResetTokenExpiration FROM Users WHERE Email = @Email";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new User
                            {
                                Id = reader.GetInt32(0),   // Id
                                Name = reader.GetString(1),   // Name
                                Email = reader.GetString(2),  // Email
                                PasswordHash = reader.GetString(3), // PasswordHash
                                CreatedAt = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4), // Handle NULL values
                                ResetToken = reader.IsDBNull(5) ? null : reader.GetString(5),  // Ensure proper NULL handling
                                ResetTokenExpiration = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6) // Ensure proper NULL handling
                            };
                        }
                        return null;
                    }
                }
            }
        }


        // Get User Profile by UserId
        public async Task<User> GetUserProfileAsync(int userId)
        {
            const string query = "SELECT Id, Name, Email, PasswordHash, CreatedAt, ResetToken, ResetTokenExpiration FROM Users WHERE Id = @UserId";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new User
                            {
                                Id = reader.GetInt32(0),   // Id (int)
                                Name = reader.GetString(1),   // Name (string)
                                Email = reader.GetString(2),  // Email (string)
                                PasswordHash = reader.GetString(3), // PasswordHash (string)
                                CreatedAt = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4), // CreatedAt (DateTime, safe null handling)
                                ResetToken = reader.IsDBNull(5) ? null : reader.GetString(5),  // ResetToken (nullable string)
                                ResetTokenExpiration = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6) // ResetTokenExpiration (nullable DateTime)
                            };
                        }
                        return null;
                    }
                }
            }
        }



        // Mark Notification as Read
        public async Task<bool> MarkNotificationAsReadAsync(int notificationId)
        {
            const string query = "UPDATE Notifications SET Read = 1 WHERE Id = @Id";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", notificationId);

                    var rowsAffected = await command.ExecuteNonQueryAsync();

                    return rowsAffected > 0;
                }
            }
        }
      

        // Get User Notifications
        public async Task<List<Notification>> GetUserNotificationsAsync(int userId)
        {
            return await _notificationRepository.GetNotificationsByUserIdAsync(userId);
        }

        // Helper methods
        // Replace the custom HashPassword and VerifyPassword methods with the following:
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }
        public bool VerifyPassword(string enteredPassword, string storedHash)
        {
            return BCrypt.Net.BCrypt.Verify(enteredPassword, storedHash);
        }

        // Store Password Reset Token
        public async Task StorePasswordResetTokenAsync(int userId, string resetToken)
        {
            const string query = "UPDATE Users SET ResetToken = @ResetToken, ResetTokenExpiration = @Expiration WHERE Id = @UserId";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ResetToken", (object)resetToken ?? DBNull.Value); // Handle NULL values safely
                    command.Parameters.AddWithValue("@Expiration", DateTime.UtcNow.AddHours(1)); // Set expiration time (1 hour)
                    command.Parameters.AddWithValue("@UserId", userId);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        throw new Exception("Failed to store password reset token.");
                    }
                }
            }
        }

        //update password
        public async Task<bool> UpdateUserPasswordAsync(UpdatePasswordRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.PasswordHash) || string.IsNullOrEmpty(request.NewPassword))
            {
                return false; // Invalid input
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                const string selectQuery = "SELECT Id, PasswordHash FROM Users WHERE Email = @Email";
                User user = null;

                using (var command = new SqlCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@Email", request.Email);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            user = new User
                            {
                                Id = reader.GetInt32(0),
                                PasswordHash = reader.GetString(1) // Ensure this is always a **hashed password**
                            };
                        }
                    }
                }

                if (user == null)
                {
                    throw new Exception("User not found");
                }

                // Verify the old password using BCrypt
                if (!BCrypt.Net.BCrypt.Verify(request.PasswordHash, user.PasswordHash))
                {
                    throw new Exception("Old password is incorrect");
                }

                // Hash the new password using BCrypt
                string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);

                const string updateQuery = "UPDATE Users SET PasswordHash = @NewPasswordHash, UpdatedAt = @UpdatedAt WHERE Id = @UserId";
                using (var command = new SqlCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@NewPasswordHash", newPasswordHash);
                    command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@UserId", user.Id);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        throw new Exception("Password update failed. No rows affected.");
                    }
                }

                return true;
            }
        }

        // Get User by Reset Token
        public async Task<User> GetUserByResetTokenAsync(string resetToken)
        {
            const string query = "SELECT * FROM Users WHERE ResetToken = @ResetToken AND ResetTokenExpiration > @CurrentTime";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ResetToken", resetToken);
                    command.Parameters.AddWithValue("@CurrentTime", DateTime.UtcNow);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new User
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Email = reader.GetString(2),
                                PasswordHash = reader.GetString(3),
                                CreatedAt = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4),
                                ResetToken = reader.IsDBNull(5) ? null : reader.GetString(5),
                                ResetTokenExpiration = reader.IsDBNull(6) ? DateTime.MinValue : reader.GetDateTime(6)
                            };
                        }
                        return null;
                    }
                }
            }
        }
    }
}

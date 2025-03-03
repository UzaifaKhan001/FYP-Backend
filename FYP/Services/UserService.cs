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
        private readonly IPasswordHasher<User> _passwordHasher;


        public UserService(
            IConfiguration configuration, // Inject IConfiguration
            NotificationRepository notificationRepository,
            IPasswordHasher<User> passwordHasher)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? throw new ArgumentNullException("Connection string is missing.");

            _notificationRepository = notificationRepository;
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

                            // Log user details for debugging
                            Console.WriteLine($"User Found: {user.Email}");
                            Console.WriteLine($"Stored Hash: {user.PasswordHash}");

                            // Verify the password using BCrypt
                            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                            {
                                Console.WriteLine($"Entered Password: {password}");
                                Console.WriteLine("Password verification failed.");
                                throw new UnauthorizedAccessException("Invalid credentials");
                            }

                            Console.WriteLine("Password verification succeeded.");
                            return user;
                        }
                        else
                        {
                            Console.WriteLine($"No user found for email: {email}");
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


        // Get User Profile by UserId
        public async Task<User> GetUserProfileAsync(int userId)
        {
            const string query = "SELECT Id, Name, Email, CreatedAt, ResetToken, ResetTokenExpiration FROM Users WHERE Id = @UserId";

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
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Email = reader.GetString(2),
                                CreatedAt = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3),
                                ResetToken = reader.IsDBNull(4) ? null : reader.GetString(4),
                                ResetTokenExpiration = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5)
                            };
                        }
                    }
                }
            }
            return null;
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

        public async Task<User> GetUserByEmailAsync(string email)
        {
            const string query = "SELECT Id, Email, PasswordHash, CreatedAt, ResetToken, ResetTokenExpiration FROM Users WHERE Email = @Email";

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
                                Id = reader.GetInt32(0),
                                Email = reader.GetString(1),
                                PasswordHash = reader.GetString(2),
                                CreatedAt = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                                ResetToken = reader.IsDBNull(4) ? null : reader.GetString(4),
                                ResetTokenExpiration = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5) // ✅ Fix null issue
                            };
                        }
                        return null;
                    }
                }
            }
        }

        public async Task<bool> StorePasswordResetTokenAsync(int userId, string token)
        {
            const string query = "UPDATE Users SET ResetToken = @Token, ResetTokenExpiration = @Expiration, UpdatedAt = @UpdatedAt WHERE Id = @UserId";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Token", token);
                    command.Parameters.AddWithValue("@Expiration", DateTime.UtcNow.AddHours(1)); // ✅ Token valid for 1 hour
                    command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@UserId", userId);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }

        //update password
        public async Task<bool> UpdateUserPasswordAsync(UpdatePasswordRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                const string selectQuery = "SELECT Id, PasswordHash FROM Users WHERE Id = @UserId";
                User user = null;

                using (var command = new SqlCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@UserId", request.Id);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            user = new User
                            {
                                Id = reader.GetInt32(0),
                                PasswordHash = reader.GetString(1)
                            };
                        }
                    }
                }

                if (user == null)
                {
                    throw new Exception("User not found");
                }

                // Verify old password using BCrypt
                if (!BCrypt.Net.BCrypt.Verify(request.PasswordHash, user.PasswordHash))
                {
                    throw new Exception("Old password is incorrect");
                }

                // Hash new password
                string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);

                const string updateQuery = "UPDATE Users SET Name = @NewName, Email = @NewEmail, PasswordHash = @NewPasswordHash, UpdatedAt = @UpdatedAt WHERE Id = @UserId";

                using (var command = new SqlCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@NewPasswordHash", newPasswordHash);
                    command.Parameters.AddWithValue("@NewEmail", request.Email);
                    command.Parameters.AddWithValue("@NewName", request.Name);
                    command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@UserId", user.Id);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }
        // upadet reset password 
        public async Task<bool> UpdateUserPasswordResetTokenAsync(string email, string token, string newPasswordHash)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(newPasswordHash))
            {
                return false;
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                const string updateQuery = @"
            UPDATE Users 
            SET PasswordHash = @NewPasswordHash, 
                ResetToken = NULL, 
                ResetTokenExpiration = NULL, 
                UpdatedAt = @UpdatedAt 
            WHERE Email = @Email AND ResetToken = @Token";

                using (var command = new SqlCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@NewPasswordHash", newPasswordHash);
                    command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Token", token);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
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

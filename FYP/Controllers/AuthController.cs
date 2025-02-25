using FYP.Services;
using FYP.Models;
using FYP.Repository;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.Data;

namespace FYP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly JwtService _jwtService;
        private readonly UserSettingsRepository _userSettingsRepository;
        private readonly NotificationRepository _notificationRepository;
        private readonly UserRepository _userRepository;
        private readonly EmailService _emailSender;

        public AuthController(UserService userService, EmailService emailSender, JwtService jwtService, UserSettingsRepository userSettingsRepository, NotificationRepository notificationRepository, UserRepository userRepository)
        {
            _userService = userService;
            _jwtService = jwtService;
            _userSettingsRepository = userSettingsRepository;
            _emailSender = emailSender;
            _notificationRepository = notificationRepository;
            _userRepository = userRepository;
        }

        // Register action
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] FYP.Models.RegisterRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.PasswordHash))
            {
                return BadRequest(new { message = "Invalid registration data." });
            }

            try
            {
                var user = await _userService.RegisterAsync(request.Name, request.Email, request.PasswordHash);
                if (user == null)
                {
                    return StatusCode(500, new { message = "User registration failed." });
                }

                // Insert user settings (async method)
                var userSettings = new UserSettings
                {
                    UserId = user.Id,
                    EmailNotifications = true,
                    PushNotifications = true,
                    Updates = true,
                    ProfileVisibility = "Public",
                    ActivityStatus = true,
                    EnableSound = true,
                    Volume = 50
                };

                bool isSettingsSaved = await _userSettingsRepository.InsertUserSettingsAsync(userSettings);
                if (!isSettingsSaved)
                {
                    return StatusCode(500, new { message = "User registered but failed to save settings." });
                }

                // Send Welcome Email
                var emailResult = await _emailSender.SendEmailAsync(user.Email, "Welcome to Our Service", "Thank you for registering!");
                if (!emailResult)
                {
                    return StatusCode(500, new { message = "Failed to send welcome email." });
                }

                return CreatedAtAction(nameof(GetUserProfile), new { userId = user.Id }, user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing your registration.", details = ex.Message });
            }
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] FYP.Models.LoginRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.PasswordHash))
            {
                return BadRequest(new { message = "Invalid login data." });
            }

            try
            {
                var user = await _userService.AuthenticateAsync(request.Email, request.PasswordHash);
                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid credentials." });
                }

                // Generate JWT Token
                string token = _jwtService.GenerateJwtToken(user);

                return Ok(new
                {
                    message = "Login successful.",
                    token, // Include JWT token in response
                    user = new
                    {
                        user.Id,
                        user.Name,
                        user.Email,
                    }
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while logging in.", details = ex.Message });
            }
        }


        // Forgot Password action
        [HttpPost("forget-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] FYP.Models.ForgotPasswordRequest request)
        {
            try
            {
                var user = await _userService.GetUserByEmailAsync(request.Email);
                if (user == null)
                {
                    return BadRequest(new { message = "User not found." });
                }

                var resetToken = Guid.NewGuid().ToString();
                await _userService.StorePasswordResetTokenAsync(user.Id, resetToken);

                var resetLink = $"http://localhost:5173//reset-password?token={resetToken}";
                await _emailSender.SendEmailAsync(user.Email, "Password Reset Request", $"Click the link to reset your password: <a href='{resetLink}'>Reset Password</a>");

                return Ok(new { message = "Password reset link sent to your email." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing your request.", details = ex.Message });
            }
        }


        // Get User Profile action
        [HttpGet("profile/{userId}")]
        public async Task<IActionResult> GetUserProfile(int userId)
        {
            try
            {
                var user = await _userService.GetUserProfileAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "Profile not found." });
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred.", details = ex.Message });
            }
        }

        // Update User Settings
        [HttpPut("update-settings")]
        public async Task<IActionResult> UpdateUserSettings([FromBody] UserSettings settings)
        {
            if (settings == null || settings.UserId <= 0)
            {
                return BadRequest(new { message = "Invalid user settings data." });
            }

            try
            {
                bool isUpdated = await _userSettingsRepository.UpdateUserSettingsAsync(settings);
                if (!isUpdated)
                {
                    return StatusCode(500, new { message = "Failed to update user settings." });
                }
                return Ok(new { message = "User settings updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred.", details = ex.Message });
            }
        }


        // Update Password
        [HttpPut("update-password")]
        public async Task<IActionResult> UpdateUserPassword([FromBody] UpdatePasswordRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.PasswordHash) || string.IsNullOrEmpty(request.NewPassword))
            {
                return BadRequest(new { message = "Invalid password update data." });
            }

            try
            {
                bool isPasswordUpdated = await _userService.UpdateUserPasswordAsync(request);
                if (!isPasswordUpdated)
                {
                    return BadRequest(new { message = "Failed to update password. Incorrect old password or update failed." });
                }

                return Ok(new { message = "Password updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating password.", error = ex.Message });
            }
        }

        // Get User Notifications action
        [HttpGet("notifications/{userId}")]
        public async Task<IActionResult> GetUserNotifications(int userId)
        {
            try
            {
                var notifications = await _userService.GetUserNotificationsAsync(userId);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Notifications retrieval failed.", details = ex.Message });
            }
        }

        // Send Test Email
        [HttpPost("send-email")]
        public async Task<IActionResult> SendTestEmail()
        {
            try
            {
                await _emailSender.SendEmailAsync("recipient@example.com", "Test Subject", "Test Body");
                return Ok("Email sent successfully!");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error sending email: {ex.Message}");
            }
        }

        // Mark Notification as Read
        [HttpPost("notifications/mark-read/{notificationId}")]
        public async Task<IActionResult> MarkNotificationAsRead(int notificationId)
        {
            try
            {
                var result = await _userService.MarkNotificationAsReadAsync(notificationId);
                if (result)
                {
                    return Ok(new { message = "Notification marked as read." });
                }
                return NotFound(new { message = "Notification not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while marking the notification.", details = ex.Message });
            }
        }
    }
}
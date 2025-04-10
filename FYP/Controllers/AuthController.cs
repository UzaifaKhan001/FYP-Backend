using FYP.Services;
using FYP.Models;
using FYP.Repository;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace FYP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly JwtService _jwtService;
        private readonly NotificationRepository _notificationRepository;
        private readonly EmailService _emailSender;

        public AuthController(
            UserService userService,
            EmailService emailSender,
            JwtService jwtService,
            NotificationRepository notificationRepository)
        {
            _userService = userService;
            _jwtService = jwtService;
            _emailSender = emailSender;
            _notificationRepository = notificationRepository;
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { message = "Auth service is running." });
        }

        // Register action
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Invalid registration data." });
            }

            try
            {
                var user = await _userService.RegisterAsync(request.Name, request.Email, request.Password, request.BusinessType);
                if (user == null)
                {
                    return StatusCode(500, new { message = "User registration failed." });
                }

                string emailBody = $"Hello {user.Name},<br/><br/>"
                                 + "Welcome to [Voice OF Customer]! Your registration was successful.<br/><br/>"
                                 + "You can now log in using your credentials.<br/><br/>"
                                 + "Best Regards,<br/>[Voice OF Customer] Team";

                bool emailResult = await _emailSender.SendEmailAsync(user.Email, "Registration Successful", emailBody);
                if (!emailResult)
                {
                    return StatusCode(500, new { message = "Failed to send welcome email." });
                }

                return CreatedAtAction(nameof(GetUserProfile), new { userId = user.Id }, user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during registration.", details = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Invalid login data." });
            }

            try
            {
                var user = await _userService.AuthenticateAsync(request.Email, request.Password);
                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid credentials." });
                }

                string token = _jwtService.GenerateJwtToken(user);

                // Generate password reset link
                var resetToken = Guid.NewGuid().ToString();
                await _userService.StorePasswordResetTokenAsync(user.Id, resetToken);
                var resetLink = $"http://localhost:5173/reset-password?email={Uri.EscapeDataString(user.Email)}&token={resetToken}";

                // Construct login notification email with reset password link
                string emailBody = $"Hello {user.Name},<br/><br/>"
                                   + "You have successfully logged into your account.<br/><br/>"
                                   + "If this was not you, please reset your password immediately by clicking the link below:<br/><br/>"
                                   + $"<a href='{resetLink}'>Reset Your Password</a><br/><br/>"
                                   + "Best Regards,<br/>Voice OF Customer Team";

                bool emailSent = await _emailSender.SendEmailAsync(user.Email, "Login Notification", emailBody);

                if (!emailSent)
                {
                    return StatusCode(500, new { message = "Login successful, but failed to send login notification email." });
                }

                return Ok(new
                {
                    message = "Login successful. A notification email with a reset password link has been sent.",
                    token,
                    user = new
                    {
                        user.Id,
                        user.Name,
                        user.Email
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while logging in.", details = ex.Message });
            }
        }


        // Forgot Password action
        [HttpPost("forget-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
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

                var resetLink = $"http://localhost:5173/reset-password?email={Uri.EscapeDataString(user.Email)}&token={resetToken}";
                var emailBody = $"<p>Click the link below to reset your password:</p><p><a href='{resetLink}'>Reset Password</a></p>";

                bool emailSent = await _emailSender.SendEmailAsync(user.Email, "Password Reset Request", emailBody);
                if (!emailSent)
                {
                    return StatusCode(500, new { message = "Failed to send reset email." });
                }

                return Ok(new { message = "Password reset link sent to your email." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing your request.", details = ex.Message });
            }
        }
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] Models.ResetPasswordRequest request)
        {
            try
            {
                var user = await _userService.GetUserByEmailAsync(request.Email);
                if (user == null)
                {
                    return BadRequest(new { message = "User not found." });
                }

                // Validate the reset token
                if (string.IsNullOrEmpty(user.ResetToken) || user.ResetToken != request.Token)
                {
                    return BadRequest(new { message = "Invalid reset token. Please check your email and try again." });
                }

                if (!user.ResetTokenExpiration.HasValue || user.ResetTokenExpiration < DateTime.UtcNow)
                {
                    return BadRequest(new { message = "Reset link has expired. Please request a new one." });
                }

                // Hash the new password using BCrypt
                string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);

                // Update the password and clear the reset token
                bool passwordUpdated = await _userService.UpdateUserPasswordResetTokenAsync(request.Email, request.Token, newPasswordHash);

                if (!passwordUpdated)
                {
                    return StatusCode(500, new { message = "Password update failed." });
                }

                // Send notification email
                string emailBody = $"Hello {user.Name},<br/><br/>"
                                 + "Your password has been successfully reset.<br/><br/>"
                                 + "If you did not request this change, please contact our support immediately.<br/><br/>"
                                 + "Best Regards,<br/>Voice OF Customer Team";

                bool emailSent = await _emailSender.SendEmailAsync(user.Email, "Password Reset Successful", emailBody);
                if (!emailSent)
                {
                    return StatusCode(500, new { message = "Password reset successful, but failed to send notification email." });
                }

                return Ok(new { message = "Password has been reset successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred.", details = ex.Message });
            }
        }

        [HttpPost("verify-reset-token")]
        public async Task<IActionResult> VerifyResetToken([FromBody] VerifyTokenRequest request)
        {
            try
            {
                var user = await _userService.GetUserByEmailAsync(request.Email);
                if (user == null)
                {
                    return Ok(new { isValid = false, message = "User not found." });
                }

                // Validate the reset token
                if (string.IsNullOrEmpty(user.ResetToken) || user.ResetToken != request.Token)
                {
                    return Ok(new { isValid = false, message = "Invalid reset token." });
                }

                if (!user.ResetTokenExpiration.HasValue || user.ResetTokenExpiration < DateTime.UtcNow)
                {
                    return Ok(new { isValid = false, message = "Reset token has expired." });
                }

                return Ok(new { isValid = true, message = "Token is valid." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while verifying token." });
            }
        }

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

        [HttpPut("update-password")]
        public async Task<IActionResult> UpdateUserPassword([FromBody] UpdatePasswordRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Name) ||
                string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.PasswordHash) ||
                string.IsNullOrEmpty(request.NewPassword))
            {
                return BadRequest(new { message = "Invalid password update data." });
            }

            try
            {
                bool isUpdated = await _userService.UpdateUserPasswordAsync(request);
                if (!isUpdated)
                {
                    return BadRequest(new { message = "Failed to update password. Incorrect old password or update failed." });
                }

                // Send notification email
                string emailBody = $"Hello {request.Name},<br/><br/>"
                                 + "Your password has been successfully updated.<br/><br/>"
                                 + "If you did not make this change, please contact our support immediately.<br/><br/>"
                                 + "Best Regards,<br/>Voice OF Customer Team";

                bool emailSent = await _emailSender.SendEmailAsync(request.Email, "Password Update Successful", emailBody);
                if (!emailSent)
                {
                    return StatusCode(500, new { message = "Password updated, but failed to send notification email." });
                }

                return Ok(new { message = "Settings updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the settings.", error = ex.Message });
            }
        }
    }
}
public class VerifyTokenRequest
{
    public string Email { get; set; }
    public string Token { get; set; }
}
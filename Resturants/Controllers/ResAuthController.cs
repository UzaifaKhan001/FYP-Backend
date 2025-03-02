using Microsoft.AspNetCore.Mvc;
using Restaurants.Models;
using Restaurants.Services;
using System;
using System.Threading.Tasks;

namespace Restaurants.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class ResAuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly EmailService _emailSender;

        public ResAuthController(AuthService authService, EmailService emailSender)
        {
            _authService = authService;
            _emailSender = emailSender;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email
            };

            bool result = await _authService.RegisterAsync(user, registerDto.Password);
            if (!result)
                return BadRequest("Email or username already exists.");

            string subject = "🎉 Welcome to Voice Of Customer – Your Success Starts Here!";
            string body = $@"
                <p>Dear {user.Username},</p>
                <p>Welcome to <b>Voice Of Customer</b>! We’re thrilled to have you as part of our business community.</p>
                <p>Our goal is to empower you with the best tools, support, and resources to help you grow and succeed.</p>
                <p>Thank you for registering!</p>
                <p>Best Regards,<br>Voice Of Customer Team</p>";

            bool emailResult = await _emailSender.SendEmailAsync(user.Email, subject, body);
            if (!emailResult)
            {
                return StatusCode(500, new { message = "User registered but failed to send welcome email." });
            }

            return Ok("User registered successfully. A welcome email has been sent.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var token = await _authService.LoginAsync(loginDto.Email, loginDto.Password);
                if (token == null)
                    return Unauthorized("Invalid credentials.");

                var user = await _authService.GetUserByEmailAsync(loginDto.Email); // ✅ Retrieve user

                return Ok(new
                {
                    message = "Login successful.",
                    token,
                    user = new
                    {
                        user.Id,
                        user.Username,  // ✅ Fixed from 'UserName' to 'Username'
                        user.Email
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while logging in.",
                    details = ex.Message
                }
                );
            }
        }
        [HttpPost("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
        {
            bool result = await _authService.UpdatePasswordAsync(dto.Email, dto.OldPassword, dto.NewPassword);
            if (!result)
                return BadRequest("Incorrect old password or user not found.");

            return Ok("Password updated successfully.");
        }

        [HttpDelete("delete-user")]
        public async Task<IActionResult> DeleteUser([FromBody] DeleteUserDto dto)
        {
            bool result = await _authService.DeleteUserAsync(dto.Email);
            if (!result)
                return BadRequest("User not found.");

            return Ok("User deleted successfully.");
        }
    }
    public class UpdatePasswordDto
    {
        public string Email { get; set; }
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }

    public class DeleteUserDto
    {
        public string Email { get; set; }
    }
}
// DTO Classes
public class RegisterDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }


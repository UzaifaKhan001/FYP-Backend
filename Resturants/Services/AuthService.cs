using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Restaurants.Data;
using Restaurants.Models;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Restaurants.Services
{
    public class AuthService
    {
        private readonly DatabaseHelper _db;
        private readonly IConfiguration _config;

        public AuthService(DatabaseHelper db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task<bool> RegisterAsync(User user, string password)
        {
            return await _db.RegisterUserAsync(user, password);
        }

        public async Task<string?> LoginAsync(string email, string password)
        {
            var user = await _db.GetUserByEmailAsync(email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;

            return GenerateJwtToken(user);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _db.GetUserByEmailAsync(email);
        }

        public async Task<bool> UpdatePasswordAsync(string email, string oldPassword, string newPassword)
        {
            var user = await _db.GetUserByEmailAsync(email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
                return false; // Incorrect old password

            return await _db.UpdatePasswordAsync(email, newPassword);
        }

        public async Task<bool> DeleteUserAsync(string email)
        {
            return await _db.DeleteUserAsync(email);
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _config["JwtSettings:SecretKey"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new ArgumentNullException("JwtSettings:SecretKey", "JWT Key is missing from configuration.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var expiryMinutes = _config.GetValue<int>("JwtSettings:ExpiryMinutes", 60);

            var token = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"],
                audience: _config["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

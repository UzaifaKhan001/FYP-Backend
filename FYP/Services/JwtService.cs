using FYP.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class JwtService
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryMinutes;

    public JwtService(IConfiguration configuration)
    {
        _secret = configuration["JwtSettings:SecretKey"] ?? throw new ArgumentNullException("JWT SecretKey is missing in appsettings.json");
        _issuer = configuration["JwtSettings:Issuer"] ?? throw new ArgumentNullException("JWT Issuer is missing in appsettings.json");
        _audience = configuration["JwtSettings:Audience"] ?? throw new ArgumentNullException("JWT Audience is missing in appsettings.json");

        if (!int.TryParse(configuration["JwtSettings:ExpiryMinutes"], out _expiryMinutes))
        {
            throw new ArgumentException("JWT ExpiryMinutes is missing or invalid in appsettings.json");
        }
    }

    public string GenerateJwtToken(User user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user), "User cannot be null");

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secret);

        // Ensure that the userId is included in the token payload (e.g., as 'userId' claim)
        var claims = new[]
        {
            new Claim("userId", user.Id.ToString()),  // Make sure userId is added here
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),  // User's unique identifier (subject)
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.UniqueName, string.IsNullOrEmpty(user.Name) ? user.Email : user.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())  // JWT Token Identifier
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_expiryMinutes),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

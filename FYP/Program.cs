using FYP.Data;
using FYP.Services;
using FYP.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Identity;
using FYP.Models;

var builder = WebApplication.CreateBuilder(args);

// Retrieve the connection string from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Retrieve JWT settings
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];
var issuer = jwtSettings["Issuer"];
var audience = jwtSettings["Audience"];

if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
{
    throw new ArgumentException("JWT settings are missing from configuration.");
}

var key = Encoding.UTF8.GetBytes(secretKey);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT Authentication Service
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Set to true in production
    options.SaveToken = true; // Save token for validation
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero // Prevents allowing expired tokens for some extra time
    };
});

// CORS policy (adjust as needed)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        policy => policy.WithOrigins("http://localhost:5173") // Add your frontend URL here
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
});

// Register repositories and services
builder.Services.AddSingleton<DbHelper>(); // Assuming it doesn't need parameters
builder.Services.AddScoped<NotificationRepository>(provider => new NotificationRepository(connectionString));
builder.Services.AddTransient<EmailService>(); // Ensure EmailService is added here
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<UserService>(); // Ensure UserService is registered correctly
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>(); // Register IPasswordHasher<User>

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowSpecificOrigin");
app.UseAuthentication(); // Ensure authentication is applied
app.UseAuthorization();

app.MapControllers(); // This will map all controller routes (including ReviewController)

app.Run();

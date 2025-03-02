using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Restaurants.Data;
using Restaurants.Services; // Ensure this namespace is correct
using System;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
var configuration = builder.Configuration;

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register necessary services
builder.Services.AddScoped<DatabaseHelper>(); // Ensure DatabaseHelper is registered
builder.Services.AddScoped<AuthService>(); // Register AuthService
builder.Services.AddScoped<RestaurantService>(); // Register  RestaurantService
builder.Services.AddTransient<EmailService>();// Register  EmailService

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// Ensure JWT settings exist
var secretKey = configuration["JwtSettings:SecretKey"];
if (string.IsNullOrEmpty(secretKey))
{
    throw new ArgumentNullException("JwtSettings:SecretKey is missing in appsettings.json");
}

// Configure JWT Authentication
var key = Encoding.UTF8.GetBytes(secretKey);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = configuration["JwtSettings:Issuer"],
            ValidAudience = configuration["JwtSettings:Audience"]
        };
    });

var app = builder.Build();

// Enable Swagger
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Restaurant API V1");
        c.RoutePrefix = "swagger";
    });
}

// Middleware setup
app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers(); // Ensure API controllers are mapped

app.Run();

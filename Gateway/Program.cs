using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add logging to help with debugging
builder.Logging.AddConsole();

// Add Ocelot configuration
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Add Ocelot service
builder.Services.AddOcelot();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        policy => policy.WithOrigins("http://localhost:5173") // Add your frontend URL here
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

// Add Swagger configuration (but Ocelot doesn't generate its own Swagger)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Ocelot API Gateway",
        Version = "v1"
    });
});

var app = builder.Build();

// Enable CORS
app.UseCors("AllowSpecificOrigin");

// Use Swagger for API documentation (but only for gateway metadata, not Ocelot routing itself)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ocelot Gateway v1");
});

// Use Ocelot middleware
await app.UseOcelot();

// Run the application
app.Run();

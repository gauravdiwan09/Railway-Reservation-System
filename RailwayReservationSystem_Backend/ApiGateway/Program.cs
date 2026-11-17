// using AuthService.Data;
// using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Identity; // Added: Brings in the official PasswordHasher

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT key is not configured.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("JWT issuer is not configured.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("JWT audience is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// --- 1. CORE SYSTEM CONFIGURATION ---
builder.Services.AddControllers(); // FIXED: Added to map your API Controllers
// builder.Services.AddOpenApi(); //register things into dependency injection
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter JWT token"
        });
 
    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

// Configure YARP Reverse Proxy
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(
        builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// --- 4. MIDDLEWARE PIPELINE ---
app.UseHttpsRedirection();

// FIXED: Middleware ordering must happen here
app.UseAuthentication(); 
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
 
        options.SwaggerEndpoint(
        "/swagger-docs/Auth/swagger.json",
        "Auth Service");
 
        options.SwaggerEndpoint(
            "/swagger-docs/Train/swagger.json",
            "Train Service");
        
        options.SwaggerEndpoint(
            "/swagger-docs/Booking/swagger.json",
            "Booking Service");
 
        options.DocumentTitle =
            "Railway Reservation Service API Gateway";
    }); //points to /swagger/v1/swagger.json
    // app.UseSwaggerUI();
}

app.MapControllers(); // FIXED: Binds your controllers to the routing engine

app.MapReverseProxy();

app.Run();

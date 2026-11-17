using Microsoft.EntityFrameworkCore;
using BookingService.Data;
using BookingService.Repositories.Interfaces;
using BookingService.Services.BookingService;
using BookingService.Services.PnrGeneratorService;
using BookingService.Services.TicketPdfGeneratorService;
using BookingService.Services.TrainServiceClientService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi;
using BookingService.Exceptions.Handler;
using BookingService.Services.RazorpayClientService;


var builder = WebApplication.CreateBuilder(args);

var envFilePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env"));
if (File.Exists(envFilePath))
{
    DotNetEnv.Env.Load(envFilePath);
    builder.Configuration.AddEnvironmentVariables();
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"];

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey!))
        };
    });

builder.Services.AddAuthorization();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
//for scope
builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("BookingConnection")));

builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IPnrGeneratorService, PnrGeneratorService>();
builder.Services.AddHttpClient<ITrainServiceClient, TrainServiceClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5002");
});
builder.Services.AddHttpClient<IRazorpayClient, RazorpayClient>(client =>
{
    client.BaseAddress = new Uri("https://api.razorpay.com/v1/");
});
builder.Services.AddScoped<IBookingService, BookingService.Services.BookingService.BookingService>();
builder.Services.AddScoped<ITicketPdfGenerator, TicketPdfGenerator>();

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

var app = builder.Build();

// --- 4. MIDDLEWARE PIPELINE ---
app.UseHttpsRedirection();
app.UseExceptionHandler();

// FIXED: Middleware ordering must happen here
app.UseAuthentication(); 
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();

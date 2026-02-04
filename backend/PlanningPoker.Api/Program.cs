using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using PlanningPoker.Api.Data;
using PlanningPoker.Api.Hubs;
using PlanningPoker.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add rate limiting services
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("CreateSessionPolicy", opt =>
    {
        opt.PermitLimit = 50;
        opt.Window = TimeSpan.FromHours(1);
    });

    options.AddFixedWindowLimiter("SubmitVotePolicy", opt =>
    {
        opt.PermitLimit = 1;
        opt.Window = TimeSpan.FromSeconds(1);
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
    };
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://localhost:8080")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure Entity Framework
builder.Services.AddDbContext<PlanningPokerDbContext>(options =>
    options.UseInMemoryDatabase("PlanningPokerDb"));

// Register services
builder.Services.AddScoped<IPinGenerator, PinGenerator>();
builder.Services.AddHostedService<SessionCleanupService>();

// Add SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseCors("AllowVueApp");

app.UseAuthorization();

app.MapControllers();

// Map SignalR hub
app.MapHub<PlanningPokerHub>("/planningpokerhub");

app.Run();

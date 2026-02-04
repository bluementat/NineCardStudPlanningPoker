using Microsoft.EntityFrameworkCore;
using PlanningPoker.Api.Data;
using PlanningPoker.Api.Hubs;
using PlanningPoker.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.UseCors("AllowVueApp");

app.UseAuthorization();

app.MapControllers();

// Map SignalR hub
app.MapHub<PlanningPokerHub>("/planningpokerhub");

app.Run();

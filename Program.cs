using PotatoServer.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Register the DbContext to use SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=envsettings.db")); // The database file path

builder.Services.AddSingleton<PotatoServer.Services.WebSocketManager>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Enable WebSockets
app.UseWebSockets();

app.MapControllers();

// WebSocket endpoint
app.Map("/ws", async context =>
{
    var wsManager = context.RequestServices.GetRequiredService<PotatoServer.Services.WebSocketManager>();
    await wsManager.HandleWebSocket(context);
});

app.UseSwagger();
app.UseSwaggerUI();

app.Run();

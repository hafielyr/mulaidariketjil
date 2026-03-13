using InvestmentGame.Server.Hubs;
using InvestmentGame.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSignalR(options =>
{
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});

// Register data services and GameEngine as singletons (in-memory state)
builder.Services.AddSingleton<StockDataService>();
builder.Services.AddSingleton<GoldDataService>();
builder.Services.AddSingleton<IndexDataService>();
builder.Services.AddSingleton<DepositoDataService>();
builder.Services.AddSingleton<BondDataService>();
builder.Services.AddSingleton<CryptoDataService>();
builder.Services.AddSingleton<GameEngine>();
builder.Services.AddSingleton<RoomManager>();

// Configure CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseCors();

// Map SignalR hub
app.MapHub<GameHub>("/gamehub");

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

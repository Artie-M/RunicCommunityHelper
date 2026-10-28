using RunicCommunityHelper.Common.Services;
using RunicCommunityHelper.Common.Database;
using RunicCommunityHelper.Common.Utils;
using RunicCommunityHelper.Implementations;
using Microsoft.EntityFrameworkCore;
using RunicCommunityHelper.Discord;
using RunicCommunityHelper.EndpointManagers;
using RunicCommunityHelper.Json;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

var cts = new CancellationTokenSource();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(cts);
builder.Services.AddSerilog();
builder.Services.AddDataStore(builder.Configuration);
builder.Services.AddDiscord(builder.Configuration);
builder.Services.AddInteractionLoader();
builder.Services.AddDiscordExceptionHandler();
builder.Services.AddJwtBearerAuth();
builder.Services.AddGoogleDriveManager();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new UInt64StringJsonConverter()));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

AuthEndpointManager.Singleton.RegisterEndpoints(app);
DiscordUsersEndpointManager.Singleton.RegisterEndpoints(app);
GoogleDocumentsEndpointManager.Singleton.RegisterEndpoints(app);

app.Map("/api/{**path}", context =>
{
    context.Response.StatusCode = StatusCodes.Status404NotFound;
    return Task.CompletedTask;
});
app.MapFallbackToFile("index.html");

var isDevelopment = app.Environment.IsDevelopment();

if (isDevelopment)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ICoreContext>();
    await db.Database.MigrateAsync();
    
    var admin = await db.Staff.FirstOrDefaultAsync(x => x.Username == "admin");

    if (admin == null)
    {
        var validator = scope.ServiceProvider.GetRequiredService<IUserValidator>();
        await validator.PostUserAsync(db,
            EnvironmentSecrets.Get("ADMIN_PASSWORD") ?? "admin",
            "admin",
            "Admin");
    }
}

app.Run();

using RunicCommunityHelper.Common.Services;
using RunicCommunityHelper.Common.Database;
using RunicCommunityHelper.Common.Utils;
using RunicCommunityHelper.Implementations;
using Microsoft.EntityFrameworkCore;
using RunicCommunityHelper.Discord;
using RunicCommunityHelper.EndpointManagers;
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

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.UseStaticFiles();

AuthEndpointManager.Singleton.RegisterEndpoints(app);
DiscordUsersEndpointManager.Singleton.RegisterEndpoints(app);
GoogleDocumentsEndpointManager.Singleton.RegisterEndpoints(app);

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

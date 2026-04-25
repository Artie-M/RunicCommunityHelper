using RunicCommunityHelper.Common.Services;
using RunicCommunityHelper.Common.Database;
using RunicCommunityHelper.EndpointManagers;
using RunicCommunityHelper.Implementations;
using Microsoft.EntityFrameworkCore;
using RunicCommunityHelper.Discord;
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

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.UseStaticFiles();

AuthEndpointManager.Singleton.RegisterEndpoints(app);

app.MapGet("/", async (HttpContext context) =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.WriteAsync("Hello World!");
});

var isDevelopment = app.Environment.IsDevelopment();

if (isDevelopment)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ICoreContext>();
    db.Database.EnsureCreated();
    await db.Database.MigrateAsync();
    
    var admin = await db.Staff.FirstOrDefaultAsync(x => x.Username == "admin");

    if (admin == null)
    {
        var validator = scope.ServiceProvider.GetRequiredService<IUserValidator>();
        await validator.PostUserAsync(db,
            Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "admin",
            "admin",
            "Admin");
    }
}

app.Run();

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RunicCommunityHelper.Common.Database;
using RunicCommunityHelper.Common.Entities;

namespace RunicCommunityHelper.EndpointManagers;

public class DiscordUsersEndpointManager
{
    public static DiscordUsersEndpointManager Singleton { get; private set; }

    static DiscordUsersEndpointManager()
    {
        Singleton = new DiscordUsersEndpointManager();
    }

    private WebApplication _app;
    
    public void RegisterEndpoints(WebApplication app)
    {
        app.MapGet("/api/discordusers", GetDiscordUsersAsync);
        app.MapGet("/api/discordusers/{id:min(0)}", GetDiscordUserAsync);
        app.MapDelete("api/discordusers/{id:min(0)}", DeleteDiscordUserAsync);
        app.MapPut("api/discordusers/{id:min(0)}", PutDiscordUserAsync);
        app.MapPost("api/discordusers/", PostDiscordUserAsync);
        _app = app;
    }

    [Authorize(Roles = "Admin")]
    private async Task GetDiscordUsersAsync(HttpContext httpContext)
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ICoreContext>();
        httpContext.Response.ContentType = "application/json; charset=utf-8";
        httpContext.Response.Headers.ContentLanguage = "ru-RU";
        
        httpContext.Response.StatusCode = StatusCodes.Status200OK;
        await httpContext.Response.WriteAsJsonAsync(await db.DiscordUsers.ToListAsync());
    }
    
    [Authorize(Roles = "Admin")]
    private async Task GetDiscordUserAsync(int id, HttpContext httpContext)
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ICoreContext>();
        var user = await db.DiscordUsers.FindAsync(id);
        
        httpContext.Response.ContentType = "application/json; charset=utf-8";
        httpContext.Response.Headers.ContentLanguage = "ru-RU";
        if (user == null)
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        
        httpContext.Response.StatusCode = StatusCodes.Status200OK;
        await httpContext.Response.WriteAsJsonAsync(user);
    }
    
    [Authorize(Roles = "Admin")] 
    private async Task DeleteDiscordUserAsync(int id, HttpContext httpContext)
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ICoreContext>();
        
        var user = await db.DiscordUsers.FindAsync(id);
        if (user == null)
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        db.DiscordUsers.Remove(user);
        await db.SaveChangesAsync();
        httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
    }

    [Authorize(Roles = "Admin")] 
    private async Task PutDiscordUserAsync(int id, HttpContext httpContext)
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ICoreContext>();
        
        var user = await httpContext.Request.ReadFromJsonAsync<DiscordUserEntity>();
        if (user == null || id != user.Id)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var existingUser = await db.DiscordUsers.FindAsync(id);
        if (existingUser == null)
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        existingUser.DiscordId = user.DiscordId;
        existingUser.Email = user.Email;

        await db.SaveChangesAsync();
        httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
    }

    [Authorize(Roles = "Admin")]
    private async Task PostDiscordUserAsync(HttpContext httpContext)
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ICoreContext>();
        
        var newUser = await httpContext.Request.ReadFromJsonAsync<DiscordUserEntity>();
        if (newUser == null)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        await db.DiscordUsers.AddAsync(newUser);
        await db.SaveChangesAsync();
        
        httpContext.Response.StatusCode = StatusCodes.Status201Created;
        await httpContext.Response.WriteAsJsonAsync(newUser);
    }
}
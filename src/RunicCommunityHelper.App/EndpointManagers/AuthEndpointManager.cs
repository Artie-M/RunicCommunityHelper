using System.Security.Claims;
using RunicCommunityHelper.Common.Database;
using RunicCommunityHelper.Common.Services;

namespace RunicCommunityHelper.EndpointManagers;

public class AuthEndpointManager
{
    public static AuthEndpointManager Singleton { get; private set; }

    static AuthEndpointManager()
    {
        Singleton = new AuthEndpointManager();
    }
    
    private WebApplication _app;
    
    public void RegisterEndpoints(WebApplication app)
    {
        _app = app;
        app.MapPost("/api/auth/login", PostAuthAsync);
    }

    private async Task PostAuthAsync(HttpContext httpContext)
    {
        var loginData = await httpContext.Request.ReadFromJsonAsync<LoginDto>();
        if (loginData == null || string.IsNullOrEmpty(loginData.Username))
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }
        
        httpContext.Response.ContentType = "application/json; charset=utf-8";
        httpContext.Response.Headers.ContentLanguage = "ru-RU";

        using var scope = _app.Services.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<IUserValidator>();
        var db = scope.ServiceProvider.GetRequiredService<ICoreContext>();

        var user = await validator.ValidateUserAsync(db, loginData.Password, loginData.Username);

        if (user == null)
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await httpContext.Response.WriteAsJsonAsync(new { error = "Неверные учетные данные" });
            return;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, "Admin"),
            new("id", user.Id.ToString())
        };

        if (user.Email != null)
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }
        
        var token = validator.GenerateSecurityToken(claims);
        httpContext.Response.StatusCode = 200;
        await httpContext.Response.WriteAsJsonAsync(new { token = token });
    }
}

record LoginDto(string Username, string Password);
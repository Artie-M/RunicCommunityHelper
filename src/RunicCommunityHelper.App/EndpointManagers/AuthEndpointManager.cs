using System.Security.Claims;
using RunicCommunityHelper.Common.Services;
using RunicCommunityHelper.Common.Database;

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

    private async Task PostAuthAsync(HttpRequest request)
    {
        var context = request.HttpContext;

        var loginData = await request.ReadFromJsonAsync<LoginDto>();
        if (loginData == null)
        {
            context.Response.StatusCode = 400; // Bad Request
            return;
        }

        using var scope = _app.Services.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<IUserValidator>();
        var db = scope.ServiceProvider.GetRequiredService<ICoreContext>();

        var user = await validator.ValidateUserAsync(db, loginData.Password, loginData.Username);

        if (user == null)
        {
            context.Response.StatusCode = 401; // Unauthorized
            await context.Response.WriteAsJsonAsync(new { error = "Неверные учетные данные" });
            return;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
            new("id", user.Id.ToString())
        };

        var token = validator.GenerateSecurityToken(claims);
        context.Response.StatusCode = 200;
        await context.Response.WriteAsJsonAsync(new { token = token });
    }
}

record LoginDto(string Username, string Password);
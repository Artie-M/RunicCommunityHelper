using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RunicCommunityHelper.Common.Database;
using RunicCommunityHelper.Common.Services;
using RunicCommunityHelper.Implementations.Database;
using RunicCommunityHelper.Implementations.Services;

namespace RunicCommunityHelper.Implementations;

public static class DependencyInjection
{
    public static IServiceCollection AddDiscordExceptionHandler(this IServiceCollection serviceCollection)
    {
        return serviceCollection.AddSingleton<IExceptionHandler, ExceptionHandler>();
    }
    
    public static IServiceCollection AddInteractionLoader(this IServiceCollection serviceCollection)
    {
        return serviceCollection.AddSingleton<IInteractionLoader, InteractionLoader>();
    }
    
    public static IServiceCollection AddDataStore(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        return serviceCollection.AddDbContext<ICoreContext, CoreContext>(x =>
        {
            x.UseSqlite(configuration.GetConnectionString("Default") 
                        ?? "Data Source=/app/data/core.db;");
        });
    }

    public static IServiceCollection AddJwtBearerAuth(this IServiceCollection serviceCollection)
    {
        var tokenGenerator = new UserValidator();
        serviceCollection.AddSingleton<IUserValidator>(tokenGenerator);
        serviceCollection.AddAuthorization();
        serviceCollection.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = UserValidator.Issuer,
                    ValidateAudience = true,
                    ValidAudience = UserValidator.Audience,
                    ValidateLifetime = true,
                    IssuerSigningKey = tokenGenerator.GetSymmetricSecurityKey(),
                    ValidateIssuerSigningKey = true
                };
            });
        return serviceCollection;
    }
}
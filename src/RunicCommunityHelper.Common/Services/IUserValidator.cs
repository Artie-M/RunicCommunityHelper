using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using RunicCommunityHelper.Common.Database;
using RunicCommunityHelper.Common.Entities;

namespace RunicCommunityHelper.Common.Services;

public interface IUserValidator
{
    SymmetricSecurityKey GetSymmetricSecurityKey();
    
    string GenerateSecurityToken(string username);
    
    string GenerateSecurityToken(IEnumerable<Claim> claims);

    string GenerateSalt(int size = 32);

    string ComputeHash(string password, string salt);
    
    Task<UserEntity?> PostUserAsync(ICoreContext db, ulong discordId, string password, string email);
    
    Task<UserEntity?> ValidateUserAsync(ICoreContext db, ulong discordId, string password);
}
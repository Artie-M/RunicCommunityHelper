using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RunicCommunityHelper.Common.Services;
using RunicCommunityHelper.Common.Database;
using RunicCommunityHelper.Common.Entities;

namespace RunicCommunityHelper.Implementations.Services;

public class UserValidator : IUserValidator
{
    public const string Issuer = "RunicCommunityHelper";
    public const string Audience = "RunicCommunityHelperClient";
    private readonly string _key = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "mysupersecret_secretsecretsecretkey!123";
    
    public SymmetricSecurityKey GetSymmetricSecurityKey()
    {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
    }
    
    public string GenerateSecurityToken(string username)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.Name, username) };
        return GenerateSecurityToken(claims);
    }
    
    public string GenerateSecurityToken(IEnumerable<Claim> claims)
    {
        var jwt = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(30)),
            signingCredentials: new SigningCredentials(GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
        
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    public string GenerateSalt(int size = 32)
    {
        var buffer = new byte[size];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(buffer);
        }
        return Convert.ToBase64String(buffer);
    }

    public string ComputeHash(string password, string salt)
    {
        var combinedPassword = password + salt;
        var bytes = Encoding.UTF8.GetBytes(combinedPassword);

        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }
    }

    public async Task<StaffUserEntity?> PostUserAsync(ICoreContext db, string password, string username, string role = "Default")
    {
        var salt = GenerateSalt();
        var hash = ComputeHash(password, salt);
        var user = new StaffUserEntity
        {
            Salt = salt,
            Password = hash,
            Username = username,
            Role = role
        };
        await db.Staff.AddAsync(user);
        await db.SaveChangesAsync();
        return user;
    }

    public async Task<StaffUserEntity?> ValidateUserAsync(ICoreContext db, string password, string username)
    {
        var user = await db.Staff.FirstOrDefaultAsync(x => x.Username == username);

        if (user == null ||
            user.Password != ComputeHash(password, user.Salt))
        {
            return null;
        }
        
        return user;
    }
}
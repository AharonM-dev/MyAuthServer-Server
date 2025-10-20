using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Api.Services;

public class TokenService(IConfiguration cfg, AppDbContext db)
{
    private readonly string _key = cfg["Jwt:Key"] ?? throw new("Missing Jwt:Key");
    private readonly int _expMin = int.Parse(cfg["Jwt:AccessMinutes"] ?? "30");
    private readonly int _refreshDays = int.Parse(cfg["Jwt:RefreshDays"] ?? "7");
    private readonly AppDbContext _db = db;

    public string CreateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("uid", user.Id.ToString())
        };
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)),
                                           SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(claims: claims,
                                         expires: DateTime.UtcNow.AddMinutes(_expMin),
                                         signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> IssueRefreshTokenAsync(User user)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var rt = new RefreshToken {
            Token = token,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshDays),
            Revoked = false
        };
        _db.RefreshTokens.Add(rt);
        await _db.SaveChangesAsync();
        return token;
    }

    public async Task<User?> ValidateRefreshAsync(string token)
    {
        var rt = await _db.RefreshTokens.Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == token && !r.Revoked && r.ExpiresAt > DateTime.UtcNow);
        return rt?.User;
    }

    public async Task RevokeRefreshAsync(string token)
    {
        var rt = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token);
        if (rt is null) return;
        rt.Revoked = true;
        await _db.SaveChangesAsync();
    }
}

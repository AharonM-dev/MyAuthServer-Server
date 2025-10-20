using Api.Data;
using Api.Dtos;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AppDbContext db, PasswordService pwd, TokenService tokens) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterDto dto)
    {
        if (await db.Users.AnyAsync(u => u.UserName == dto.UserName || u.Email == dto.Email.ToLower()))
            return BadRequest("Username or email already exists");

        var (h,s) = pwd.CreateHash(dto.Password);
        var user = new User { UserName = dto.UserName.Trim(), Email = dto.Email.Trim().ToLower(), PasswordHash = h, PasswordSalt = s };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var access = tokens.CreateAccessToken(user);
        var refresh = await tokens.IssueRefreshTokenAsync(user);
        return Ok(new AuthResponse(user.UserName, user.Email, access, refresh));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginDto dto)
    {
        var key = dto.UserNameOrEmail.Trim().ToLower();
        var user = await db.Users.FirstOrDefaultAsync(u => u.UserName.ToLower()==key || u.Email==key);
        if (user is null) return Unauthorized("Invalid credentials");
        if (!new PasswordService().Verify(dto.Password, user.PasswordHash, user.PasswordSalt)) return Unauthorized("Invalid credentials");

        var access = tokens.CreateAccessToken(user);
        var refresh = await tokens.IssueRefreshTokenAsync(user);
        return Ok(new AuthResponse(user.UserName, user.Email, access, refresh));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest req)
    {
        var user = await tokens.ValidateRefreshAsync(req.RefreshToken);
        if (user is null) return Unauthorized();

        // Revoke old and issue new (rotate)
        await tokens.RevokeRefreshAsync(req.RefreshToken);
        var access = tokens.CreateAccessToken(user);
        var refresh = await tokens.IssueRefreshTokenAsync(user);
        return Ok(new AuthResponse(user.UserName, user.Email, access, refresh));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest req)
    {
        await tokens.RevokeRefreshAsync(req.RefreshToken);
        return NoContent();
    }
}

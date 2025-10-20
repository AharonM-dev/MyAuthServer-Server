namespace Api.Dtos;
public record RegisterDto(string UserName, string Email, string Password);
public record LoginDto(string UserNameOrEmail, string Password);
public record AuthResponse(string UserName, string Email, string AccessToken, string RefreshToken);
public record RefreshRequest(string RefreshToken);

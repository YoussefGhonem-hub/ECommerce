namespace ECommerce.Application.Users.Dtos;

public record RegisterRequest(string FullName, string Email, string PhoneNumber, string Password);
public record LoginRequest(string UserNameOrEmail, string Password, bool RememberMe);
public record AuthResponse(string Token, DateTime ExpiresAt, string UserId, string Email);

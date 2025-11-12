namespace Shared.DTOs;

public record UserCreateRequest(string UserName, string Email, string Password, string Role);


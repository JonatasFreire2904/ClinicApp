namespace Shared.DTOs;

public record UserUpdateRequest(string UserName, string Email, string? Password, string Role);


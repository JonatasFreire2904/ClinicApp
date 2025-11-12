namespace Shared.DTOs;

public record UserDto(Guid Id, string UserName, string Email, string Role, DateTime CreatedAt);


using Core.Entities;
using Core.Entities.Enums;
using Infrastructure.Dat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Master")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _db.Users
            .OrderBy(u => u.UserName)
            .Select(u => new UserDto(
                u.Id,
                u.UserName,
                u.Email,
                u.Role.ToString(),
                u.CreatedAt
            ))
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        return Ok(new UserDto(
            user.Id,
            user.UserName,
            user.Email,
            user.Role.ToString(),
            user.CreatedAt
        ));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("UserName e Email são obrigatórios.");

        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Senha é obrigatória.");

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            return BadRequest($"Role inválida: {request.Role}");

        // Verifica se já existe usuário com mesmo UserName ou Email
        var exists = await _db.Users
            .AnyAsync(u => u.UserName == request.UserName || u.Email == request.Email);

        if (exists)
            return BadRequest("Já existe um usuário com este UserName ou Email.");

        var user = new User
        {
            UserName = request.UserName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = user.Id }, new UserDto(
            user.Id,
            user.UserName,
            user.Email,
            user.Role.ToString(),
            user.CreatedAt
        ));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UserUpdateRequest request)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("UserName e Email são obrigatórios.");

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            return BadRequest($"Role inválida: {request.Role}");

        // Verifica se outro usuário já usa este UserName ou Email
        var exists = await _db.Users
            .AnyAsync(u => u.Id != id && (u.UserName == request.UserName || u.Email == request.Email));

        if (exists)
            return BadRequest("Já existe outro usuário com este UserName ou Email.");

        user.UserName = request.UserName;
        user.Email = request.Email;
        user.Role = role;

        // Atualiza senha apenas se fornecida
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        await _db.SaveChangesAsync();

        return Ok(new UserDto(
            user.Id,
            user.UserName,
            user.Email,
            user.Role.ToString(),
            user.CreatedAt
        ));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        // Não permite deletar usuários Master
        if (user.Role == UserRole.Master)
            return BadRequest("Não é possível deletar usuários Master.");

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}


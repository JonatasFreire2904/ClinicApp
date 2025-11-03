using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs.Auth;
using Infrastructure.Dat;
using BCrypt.Net;
using Api.Services;

namespace Api.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly TokenService _tokenService;

        public AuthController(AppDbContext db, TokenService tokenService)
        {
            _db = db;
            _tokenService = tokenService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.UserName == request.UserName);

            if (user == null)
                return Unauthorized("Usuário não encontrado");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized("Senha inválida");

            var token = _tokenService.GenerateToken(user);

            return Ok(new LoginResponse
            {
                Token = token,
                UserName = user.UserName,
                Role = user.Role.ToString()
            });
        }
    }

}



using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs.Auth;
using Infrastructure.Dat;
using Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(AppDbContext db, TokenService tokenService) : ControllerBase
    {
        private readonly AppDbContext _db = db;
        private readonly TokenService _tokenService = tokenService;

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Dados de login não informados.");

                if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
                    return BadRequest("Usuário e senha são obrigatórios.");

                var user = await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.UserName == request.UserName);

                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                    return Unauthorized("Usuário ou senha inválidos.");

                var token = _tokenService.GenerateToken(user);

                return Ok(new LoginResponse
                {
                    Token = token,
                    UserName = user.UserName,
                    Role = user.Role.ToString()
                });
            }
            catch (Exception ex)
            {
                // Ideal: logar o erro com ILogger
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Ocorreu um erro ao realizar o login. Tente novamente mais tarde. " + ex.Message + " " + ex.StackTrace);
            }
        }

        [HttpPost("getconfig")]
        [AllowAnonymous]
        public async Task<IActionResult> Getconfig(string name)
        {
            try
            {
                return Ok(config[name]);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ex.Message + " " + ex.StackTrace);
            }
        }
    }
}



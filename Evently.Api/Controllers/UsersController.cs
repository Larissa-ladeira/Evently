using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Evently.Api.Data;
using Evently.Api.DTOs;
using Evently.Api.Models;
using Evently.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Evently.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private static readonly HashSet<string> SelfAssignableRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            "Client",
            "Organizer"
        };

        private readonly AppDbContext _context;
        private readonly SecuritySettings _security;

        public UsersController(AppDbContext context, SecuritySettings security)
        {
            _context = context;
            _security = security;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            {
                return BadRequest(new { message = "Este e-mail já está cadastrado." });
            }

            var role = SelfAssignableRoles.Contains(dto.Role) ? dto.Role : "Client";

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return StatusCode(201, new { message = "Usuário cadastrado com sucesso!", userId = user.Id });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "E-mail ou senha inválidos." });
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = _security.JwtKeyBytes;

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                Issuer = _security.JwtIssuer,
                Audience = _security.JwtAudience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new
            {
                message = "Login realizado com sucesso!",
                token = tokenString,
                userId = user.Id,
                name = user.Name,
                role = user.Role
            });
        }
    }
}

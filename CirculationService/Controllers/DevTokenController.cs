using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CirculationService.Controllers;

/// <summary>
/// Controller tạo token test — CHỈ HOẠT ĐỘNG trong môi trường Development.
/// KHÔNG có trong Production.
/// </summary>
[ApiController]
[Route("api/dev")]
public class DevTokenController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;

    public DevTokenController(IConfiguration configuration, IWebHostEnvironment env)
    {
        _configuration = configuration;
        _env = env;
    }

    /// <summary>
    /// Tạo JWT token test cho Swagger.
    /// Chỉ dùng khi Development — không có trong Production.
    /// </summary>
    /// <param name="role">Admin | Librarian | Reader</param>
    [HttpGet("token")]
    public IActionResult GetDevToken([FromQuery] string role = "Admin")
    {
        if (!_env.IsDevelopment())
        {
            return NotFound(); // Ẩn hoàn toàn trong Production
        }

        var validRoles = new[] { "Admin", "Librarian", "Reader" };
        if (!validRoles.Contains(role))
        {
            return BadRequest(new { message = "Role phải là: Admin, Librarian, hoặc Reader" });
        }

        var jwtKey = _configuration["Jwt:Key"]!;
        var issuer  = _configuration["Jwt:Issuer"]!;
        var audience = _configuration["Jwt:Audience"]!;

        var claims = new[]
        {
            // Đúng theo contract JWT của Nhóm 3:
            new Claim("userId",   "00000000-0000-0000-0000-000000000001"),
            new Claim("fullName", $"Test {role}"),
            new Claim("email",    $"test{role.ToLower()}@library.vn"),
            new Claim(ClaimTypes.Role, role),   // dùng để [Authorize(Roles="...")] hoạt động
            new Claim("role",     role),         // fallback nếu Nhóm 3 dùng claim "role"
        };

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new
        {
            role,
            token    = tokenString,
            note     = "Dùng token này trong Swagger: bấm Authorize → nhập 'Bearer {token}'",
            expireAt = DateTime.UtcNow.AddHours(8).ToString("yyyy-MM-dd HH:mm:ss UTC")
        });
    }
}

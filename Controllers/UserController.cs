using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EPinAPI.Data;
using EPinAPI.Models;
using EPinAPI.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using EPinAPI.Services;

namespace EPinAPI.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AdminLogService _logService;

        public UserController(AppDbContext context, AdminLogService logService)
        {
            _context = context;
            _logService = logService;
        }

        private async Task<IActionResult> AuthenticateUser(LoginRequest loginRequest, bool isAdminLogin = false)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginRequest.Email);

            if (user == null || !AuthService.VerifyPassword(loginRequest.Password, user.PasswordHash))
            {
                if (isAdminLogin)
                {
                    await _logService.LogAction(0, $"Başarısız admin giriş denemesi: {loginRequest.Email}", "/api/users/admin-login");
                }
                return Unauthorized(new { message = "Geçersiz email veya şifre!" });
            }
            if (!user.IsActive)
            {
                return Unauthorized(new { message = "Hesabınız devre dışı bırakılmıştır. Lütfen yöneticinizle iletişime geçin." });
            }

            if (isAdminLogin && user.Role != "Admin")
            {
                await _logService.LogAction(user.Id, "Yetkisiz admin paneline giriş denemesi", "/api/users/admin-login");
                return Forbid(); // 403 Forbidden
            }

            var accessToken = JwtService.GenerateToken(user.Id, user.Email, user.Role);
            var refreshToken = AuthService.GenerateRefreshToken();
            var deviceInfo = Request.Headers["User-Agent"].ToString(); // 📌 Kullanıcı cihaz bilgisi

            // 📌 Kullanıcının önceki refresh token'ı var mı kontrol edelim
            var existingToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.UserId == user.Id && rt.DeviceInfo == deviceInfo);
            if (existingToken != null)
            {
                existingToken.Token = refreshToken;
                existingToken.ExpiryDate = DateTime.UtcNow.AddDays(7);
            }
            else
            {
                _context.RefreshTokens.Add(new RefreshToken
                {
                    UserId = user.Id,
                    Token = refreshToken,
                    ExpiryDate = DateTime.UtcNow.AddDays(7),
                    DeviceInfo = deviceInfo
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = isAdminLogin ? "Admin Girişi başarılı!" : "Giriş başarılı!",
                accessToken,
                refreshToken,
                user = new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    user.Role
                }
            });
        }

        // 📌 Kullanıcı Oluştur
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDTO userDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); // 📌 Doğrulama hatalarını döndür
            }

            // 📌 Kullanıcı zaten var mı kontrol edelim
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userDto.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Bu email adresi zaten kullanılıyor!" });
            }

            var user = new User
            {
                Name = userDto.Name,
                Email = userDto.Email.ToLower(),
                PasswordHash = AuthService.HashPassword(userDto.Password),
                Role = "User" // 📌 Varsayılan olarak "User" atanır
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Kullanıcı başarıyla oluşturuldu!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            return await AuthenticateUser(loginRequest);
        }

        [HttpPost("admin-login")]
        public async Task<IActionResult> AdminLogin([FromBody] LoginRequest loginRequest)
        {
            return await AuthenticateUser(loginRequest, true); // 📌 Admin girişi için `isAdminLogin = true`
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue("userId");
            var deviceInfo = Request.Headers["User-Agent"].ToString();

            var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.UserId == int.Parse(userId) && (rt.DeviceInfo == deviceInfo || string.IsNullOrEmpty(deviceInfo)));

            if (refreshToken == null)
            {
                return BadRequest(new { message = "Zaten çıkış yapmışsınız veya oturum süreniz dolmuş!" });
            }

            _context.RefreshTokens.Remove(refreshToken);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Çıkış başarılı!" });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var deviceInfo = Request.Headers["User-Agent"].ToString();

            var refreshToken = await _context.RefreshTokens
                .AsNoTracking()
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.DeviceInfo == deviceInfo);

            if (refreshToken?.User == null || refreshToken.UserId <= 0)
            {
                return Unauthorized(new { message = "Kullanıcı bulunamadı!" });
            }

            if (refreshToken.ExpiryDate < DateTime.UtcNow)
            {
                _context.RefreshTokens.Remove(refreshToken);
                await _context.SaveChangesAsync();
                return Unauthorized(new { message = "Refresh token süresi dolmuş, tekrar giriş yapmalısınız!" });
            }

            var user = refreshToken.User;

            if (user == null)
            {
                return Unauthorized(new { message = "Kullanıcı bulunamadı!" });
            }

            var newAccessToken = JwtService.GenerateToken(user.Id, user.Email, user.Role);
            var newRefreshToken = AuthService.GenerateRefreshToken();

            refreshToken.Token = newRefreshToken;
            refreshToken.ExpiryDate = DateTime.UtcNow.AddDays(7);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                accessToken = newAccessToken,
                refreshToken = newRefreshToken
            });
        }

        // 📌 Tüm Kullanıcıları Getir (Sadece Admin)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .AsNoTracking()
                .Select(u => new UserDTO
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    Role = u.Role
                })
                .ToListAsync();

            return Ok(users);
        }

        // 📌 Belirli Kullanıcıyı Getir (Kendi ID’sini veya Admin yetkililer bakabilir)
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetUserById(int id)
        {
            var userId = int.Parse(User.Claims.First(c => c.Type == "userId").Value);
            var userRole = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

            if (userRole != "Admin" && userId != id)
            {
                return Forbid(); // 📌 Kullanıcı sadece kendi bilgilerini görebilir
            }

            var user = await _context.Users
                .Where(u => u.Id == id)
                .Select(u => new UserDTO { Id = u.Id, Name = u.Name, Email = u.Email, Role = u.Role })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { message = "Kullanıcı bulunamadı!" });
            }

            return Ok(user);
        }

        [HttpPatch("{id}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateUserRoleDTO request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Kullanıcı bulunamadı!" });
            }

            if (user.Role == "Admin" && request.Role != "Admin")
            {
                return BadRequest(new { message = "Admin rolü değiştirilemez!" });
            }

            user.Role = request.Role;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Kullanıcı rolü güncellendi!", user });
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusDTO request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Kullanıcı bulunamadı!" });
            }

            user.IsActive = request.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Kullanıcı {(user.IsActive ? "aktif" : "pasif")} hale getirildi!", user });
        }
    }
}

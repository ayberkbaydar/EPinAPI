using EPinAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EPinAPI.Controllers
{
    [Route("api/logs")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class LogController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LogController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("admin-actions")]
        public async Task<IActionResult> GetAdminActions()
        {
            var logs = await _context.AdminLogs.OrderByDescending(l => l.Timestamp).ToListAsync();
            return Ok(logs);
        }

        [HttpGet("failed-logins")]
        public async Task<IActionResult> GetFailedLogins()
        {
            var logs = await _context.AdminLogs
                .Where(l => l.Action.Contains("Başarısız admin giriş denemesi") || l.Action.Contains("Yetkisiz admin paneline giriş denemesi"))
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
            return Ok(logs);
        }
    }

}

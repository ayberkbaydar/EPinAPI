using EPinAPI.Data;
using EPinAPI.Models;

namespace EPinAPI.Services
{
    public class AdminLogService
    {
        private readonly AppDbContext _context;

        public AdminLogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogAction(int adminId, string action, string endpoint)
        {
            var log = new AdminLog
            {
                AdminId = adminId,
                Action = action,
                Endpoint = endpoint
            };

            _context.AdminLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }

}

using EPinAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EPinAPI.Controllers
{
    public class DashboardController:ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            var totalSales = await _context.Orders.Where(o => o.Status == "Completed").SumAsync(o => o.TotalPrice);
            var totalOrders = await _context.Orders.CountAsync();
            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => u.IsActive);

            return Ok(new
            {
                totalSales,
                totalOrders,
                totalUsers,
                activeUsers
            });
        }

        [HttpGet("sales")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSalesData([FromQuery] string range = "30d")
        {
            DateTime startDate = range switch
            {
                "7d" => DateTime.UtcNow.AddDays(-7),
                "30d" => DateTime.UtcNow.AddDays(-30),
                "1y" => DateTime.UtcNow.AddYears(-1),
                _ => DateTime.UtcNow.AddDays(-30) // Varsayılan: 30 gün
            };

            var sales = await _context.Orders
                .Where(o => o.Status == "Completed" && o.OrderDate >= startDate)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    date = g.Key,
                    totalSales = g.Sum(o => o.TotalPrice),
                    orderCount = g.Count()
                })
                .OrderBy(g => g.date)
                .ToListAsync();

            return Ok(sales);
        }

        [HttpGet("top-games")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTopSellingGames()
        {
            var topGames = await _context.Orders
                .Where(o => o.Status == "Completed")
                .GroupBy(o => o.Epin.Name)
                .Select(g => new
                {
                    gameName = g.Key,
                    totalSales = g.Count()
                })
                .OrderByDescending(g => g.totalSales)
                .Take(10)
                .ToListAsync();

            return Ok(topGames);
        }



    }
}

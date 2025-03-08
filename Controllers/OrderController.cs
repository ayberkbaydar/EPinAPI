using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EPinAPI.Data;
using EPinAPI.Models;
using Microsoft.AspNetCore.Authorization;
using EPinAPI.Models.DTOs;
using EPinAPI.Services;

namespace EPinAPI.Controllers
{
    [Route("api/orders")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        // 📌 Sipariş Oluştur
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] Order order)
        {
            var user = await _context.Users.FindAsync(order.UserId);
            if (user == null)
                return NotFound(new { message = "Kullanıcı bulunamadı!" });

            var epin = await _context.Epins.FindAsync(order.EpinId);
            if (epin == null || epin.IsSold)
                return BadRequest(new { message = "E-Pin bulunamadı veya zaten satılmış!" });

            // Kullanıcının bakiyesi yeterli mi kontrol et
            if (user.Balance < epin.Price)
                return BadRequest(new { message = "Yetersiz bakiye!" });

            // Bakiyeyi düş ve E-Pin'i satıldı olarak işaretle
            user.Balance -= epin.Price;
            epin.IsSold = true;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Sipariş başarıyla oluşturuldu!", order });
        }

        // 📌 Kullanıcının Siparişlerini Getir
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetOrdersByUser(int userId)
        {
            var orders = await _context.Orders.Where(o => o.UserId == userId).ToListAsync();
            return Ok(orders);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetOrders([FromQuery] string? status, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var userId = int.Parse(User.Claims.First(c => c.Type == "userId").Value);
            var userRole = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

            var query = _context.Orders.Include(o => o.Epin).Include(o => o.User).AsQueryable();

            if (userRole != "Admin")
            {
                query = query.Where(o => o.UserId == userId);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }
            if (startDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(o => o.OrderDate <= endDate.Value);
            }

            var orders = await query.ToListAsync();
            return Ok(orders);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var userId = int.Parse(User.Claims.First(c => c.Type == "userId").Value);
            var userRole = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

            var order = await _context.Orders
                .Include(o => o.Epin)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound(new { message = "Sipariş bulunamadı!" });
            }

            if (userRole != "Admin" && order.UserId != userId)
            {
                return Forbid(); // Kullanıcı sadece kendi siparişine bakabilir
            }

            return Ok(order);
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDTO request, [FromServices] AdminLogService logService)
        {
            var adminId = int.Parse(User.Claims.First(c => c.Type == "userId").Value);

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound(new { message = "Sipariş bulunamadı!" });
            }

            var validStatuses = new List<string> { "Pending", "Completed", "Cancelled" };
            if (!validStatuses.Contains(request.Status))
            {
                return BadRequest(new { message = "Geçersiz sipariş durumu!" });
            }

            order.Status = request.Status;
            await _context.SaveChangesAsync();

            await logService.LogAction(adminId, $"Sipariş durumu {request.Status} olarak güncellendi.", "/api/orders/{id}/status");

            return Ok(new { message = "Sipariş durumu güncellendi!", order });
        }


    }
}

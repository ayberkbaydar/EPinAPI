using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EPinAPI.Data;
using EPinAPI.Models;
using EPinAPI.Attributes;
using System.Linq;
using System.Threading.Tasks;
using EPinAPI.Models.DTOs;
using EPinAPI.Services;

namespace EPinAPI.Controllers
{
    [Route("api/epins")]
    [ApiController]
    public class EpinController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EpinController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [AuthorizeRoles("Admin")]
        public async Task<IActionResult> AddEpin([FromBody] Epin epin)
        {
            if (string.IsNullOrEmpty(epin.Name) || string.IsNullOrEmpty(epin.Code) || epin.Price <= 0)
            {
                return BadRequest(new { message = "Geçersiz e-pin bilgisi!" });
            }

            _context.Epins.Add(epin);
            await _context.SaveChangesAsync();

            return Ok(new { message = "E-Pin başarıyla eklendi!", epin });
        }

        //[Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllEpins()
        {
            var epins = await _context.Epins.Where(e => !e.IsSold).ToListAsync();
            return Ok(epins);
        }

        // 📌 Belirli E-Pin'i Getir
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEpinById(int id)
        {
            var epin = await _context.Epins.FindAsync(id);
            if (epin == null)
                return NotFound(new { message = "E-Pin bulunamadı!" });

            return Ok(epin);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEpin(int id, [FromBody] UpdateEpinDTO request)
        {
            var epin = await _context.Epins.FindAsync(id);
            if (epin == null)
            {
                return NotFound(new { message = "E-Pin bulunamadı!" });
            }

            epin.Name = request.Name ?? epin.Name;
            epin.Price = request.Price > 0 ? request.Price : epin.Price;
            epin.Code = request.Code ?? epin.Code;

            await _context.SaveChangesAsync();

            return Ok(new { message = "E-Pin başarıyla güncellendi!", epin });
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEpinStatus(int id, [FromBody] UpdateEpinStatusDTO request, [FromServices] AdminLogService logService)
        {
            var adminId = int.Parse(User.Claims.First(c => c.Type == "userId").Value);

            var epin = await _context.Epins.FindAsync(id);
            if (epin == null)
            {
                return NotFound(new { message = "E-Pin bulunamadı!" });
            }

            epin.IsActive = request.IsActive;
            await _context.SaveChangesAsync();

            await logService.LogAction(adminId, $"E-Pin {(epin.IsActive ? "Aktif" : "Pasif")} hale getirildi.", "/api/epins/{id}/status");

            return Ok(new { message = $"E-Pin {(epin.IsActive ? "aktif" : "pasif")} hale getirildi!", epin });
        }

        [HttpGet("filtered")]
        public async Task<IActionResult> GetEpins([FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice, [FromQuery] bool? isSold)
        {
            var query = _context.Epins.Where(e => e.IsActive);

            if (minPrice.HasValue)
            {
                query = query.Where(e => e.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(e => e.Price <= maxPrice.Value);
            }
            if (isSold.HasValue)
            {
                query = query.Where(e => e.IsSold == isSold.Value);
            }

            var epins = await query.ToListAsync();
            return Ok(epins);
        }



    }
}

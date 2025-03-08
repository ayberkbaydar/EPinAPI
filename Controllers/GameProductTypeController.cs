using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EPinAPI.Data;
using EPinAPI.Models;
using EPinAPI.Attributes;
using System.Linq;
using System.Threading.Tasks;
using EPinAPI.Models.DTOs;

namespace EPinAPI.Controllers
{
    [Route("api/game-product-types")]
    [ApiController]
    public class GameProductTypeController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GameProductTypeController(AppDbContext context)
        {
            _context = context;
        }

        // 📌 Belirli bir oyuna ait ürün tiplerini listeleme (Herkes erişebilir)
        [HttpGet("game/{gameId}")]
        public async Task<IActionResult> GetProductTypesByGame(int gameId)
        {
            var productTypes = await _context.GameProductTypes
                .Where(pt => pt.GameId == gameId && pt.IsActive) // 📌 Sadece aktif ürünleri getir
                .ToListAsync();

            if (!productTypes.Any())
                return NotFound(new { message = "Bu oyuna ait aktif ürün bulunamadı!" });

            return Ok(productTypes);
        }

        // 📌 Yeni ürün tipi ekleme (Sadece ADMIN)
        [HttpPost]
        [AuthorizeRoles("Admin")]
        public async Task<IActionResult> AddProductType([FromBody] GameProductTypeDTO gameProductTypeDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var game = await _context.Games.FirstOrDefaultAsync(c => c.Id == gameProductTypeDTO.GameId);
            if (game == null)
            {
                return BadRequest(new { message = "Geçersiz Game ID!" });
            }

            if (string.IsNullOrEmpty(gameProductTypeDTO.Name) || gameProductTypeDTO.GameId <= 0)
            {
                return BadRequest(new { message = "Ürün tipi adı ve bağlı olduğu oyun zorunludur!" });
            }

            var gameProductType = new GameProductType
            {
                Name = gameProductTypeDTO.Name,
                GameId = gameProductTypeDTO.GameId,
                Game = game,
                IsActive = true
            };

            _context.GameProductTypes.Add(gameProductType);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Ürün tipi başarıyla eklendi!", gameProductTypeDTO });
        }

        // 📌 Ürün tipi güncelleme (Sadece ADMIN)
        [HttpPut("{id}")]
        [AuthorizeRoles("Admin")]
        public async Task<IActionResult> UpdateProductType(int id, [FromBody] GameProductType updatedProductType)
        {
            var productType = await _context.GameProductTypes.FindAsync(id);
            if (productType == null)
                return NotFound(new { message = "Ürün tipi bulunamadı!" });

            productType.Name = updatedProductType.Name;
            productType.GameId = updatedProductType.GameId;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Ürün tipi güncellendi!", productType });
        }

        // 📌 Ürün tipi silme (Sadece ADMIN)
        [HttpDelete("{id}")]
        [AuthorizeRoles("Admin")]
        public async Task<IActionResult> SoftDeleteProductType(int id)
        {
            var productType = await _context.GameProductTypes.FindAsync(id);
            if (productType == null)
                return NotFound(new { message = "Ürün tipi bulunamadı!" });

            productType.IsActive = false; // 📌 Ürün tipini devre dışı bırakıyoruz
            await _context.SaveChangesAsync();

            return Ok(new { message = "Ürün tipi başarıyla devre dışı bırakıldı!" });
        }
    }
}

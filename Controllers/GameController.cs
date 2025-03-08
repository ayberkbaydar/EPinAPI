using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EPinAPI.Data;
using EPinAPI.Models;
using EPinAPI.Attributes;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using EPinAPI.Models.DTOs;

namespace EPinAPI.Controllers
{
    [Route("api/games")]
    [ApiController]
    public class GameController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GameController(AppDbContext context)
        {
            _context = context;
        }

        // 📌 Tüm oyunları listeleme (Herkes erişebilir)
        [HttpGet]
        public async Task<IActionResult> GetAllGames()
        {
            var games = await _context.Games
                .Where(g => g.IsActive) // 📌 Sadece aktif oyunları getir
                .Include(g => g.Category)
                .Include(g => g.ProductTypes)
                .ToListAsync();

            return Ok(games);
        }

        // 📌 Oyun ekleme (Sadece ADMIN)
        [HttpPost]
        [AuthorizeRoles("Admin")] // 🛡️ Sadece admin ekleyebilir
        public async Task<IActionResult> AddGame([FromBody] GameDTO gameDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == gameDto.CategoryId);
            if (category == null)
            {
                return BadRequest(new { message = "Geçersiz kategori ID!" });
            }

            if (string.IsNullOrEmpty(gameDto.Name) || gameDto.CategoryId <= 0)
            {
                return BadRequest(new { message = "Oyun adı ve kategori zorunludur!" });
            }
            var game = new Game
            {
                Name = gameDto.Name,
                Description = gameDto.Description,
                CategoryId = gameDto.CategoryId,
                Category = category, // EF Core için gerekli
                IsActive = true
            };

            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Oyun başarıyla eklendi!", gameDto });
        }

        // 📌 Oyun güncelleme (Sadece ADMIN)
        [HttpPut("{id}")]
        [AuthorizeRoles("Admin")]
        public async Task<IActionResult> UpdateGame(int id, [FromBody] Game updatedGame)
        {
            var game = await _context.Games.FindAsync(id);
            if (game == null)
                return NotFound(new { message = "Oyun bulunamadı!" });

            game.Name = updatedGame.Name;
            game.Description = updatedGame.Description;
            game.CategoryId = updatedGame.CategoryId;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Oyun güncellendi!", game });
        }

        // 📌 Oyun silme (Sadece ADMIN)
        [HttpDelete("{id}")]
        [AuthorizeRoles("Admin")]
        public async Task<IActionResult> SoftDeleteGame(int id)
        {
            var game = await _context.Games.FindAsync(id);
            if (game == null)
                return NotFound(new { message = "Oyun bulunamadı!" });

            game.IsActive = false; // 📌 Oyunu devre dışı bırakıyoruz
            await _context.SaveChangesAsync();

            return Ok(new { message = "Oyun başarıyla devre dışı bırakıldı!" });
        }

        [HttpGet("{gameId}/product-types")]
        public async Task<IActionResult> GetGameProductTypes(int gameId)
        {
            // 📌 Belirtilen gameId'ye sahip oyun var mı kontrol et
            var game = await _context.Games
                .Include(g => g.ProductTypes) // GameProductType'ları da çek
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null)
            {
                return NotFound(new { message = "Oyun bulunamadı!" });
            }

            // 📌 Eğer oyun bulunursa, ilgili tüm GameProductType'ları döndür
            return Ok(new
            {
                gameId = game.Id,
                gameName = game.Name,
                productTypes = game.ProductTypes.Select(pt => new
                {
                    id = pt.Id,
                    name = pt.Name
                }).ToList()
            });
        }

    }
}

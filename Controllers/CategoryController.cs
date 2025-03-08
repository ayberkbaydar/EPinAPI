using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EPinAPI.Data;
using EPinAPI.Models;
using EPinAPI.Attributes;
using System.Linq;
using System.Threading.Tasks;
using EPinAPI.Services;

namespace EPinAPI.Controllers
{
    [Route("api/categories")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoryController(AppDbContext context)
        {
            _context = context;
        }

        // 📌 Tüm kategorileri listele (Herkes erişebilir)
        [HttpGet]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _context.Categories
        .Where(c => c.IsActive) // 📌 Sadece aktif kategorileri getir
        .Include(c => c.Games)
        .ToListAsync();

            return Ok(categories);
        }

        // 📌 Yeni kategori ekleme (Sadece ADMIN)
        [HttpPost]
        [AuthorizeRoles("Admin")]
        public async Task<IActionResult> AddCategory([FromBody] Category category)
        {
            if (string.IsNullOrEmpty(category.Name))
            {
                return BadRequest(new { message = "Kategori adı boş olamaz!" });
            }

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Kategori başarıyla eklendi!", category });
        }

        // 📌 Kategori güncelleme (Sadece ADMIN)
        [HttpPut("{id}")]
        [AuthorizeRoles("Admin")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] Category updatedCategory)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound(new { message = "Kategori bulunamadı!" });

            category.Name = updatedCategory.Name;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Kategori güncellendi!", category });
        }

        // 📌 Kategori silme (Sadece ADMIN)
        [HttpDelete("{id}")]
        [AuthorizeRoles("Admin")]
        public async Task<IActionResult> SoftDeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound(new { message = "Kategori bulunamadı!" });

            // 📌 Kategoriyi pasif hale getir
            category.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Kategori başarıyla devre dışı bırakıldı!", category });
        }

    }
}

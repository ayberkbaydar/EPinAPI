namespace EPinAPI.Models
{
    public class Game
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public int CategoryId { get; set; } // Hangi kategoriye ait?
        public Category Category { get; set; }

        public bool IsActive { get; set; } = true;

        // Oyuna bağlı ürün tipleri
        public ICollection<GameProductType>? ProductTypes { get; set; }
    }
}

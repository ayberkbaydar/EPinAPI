namespace EPinAPI.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public bool IsActive { get; set; } = true;

        // Bir kategoriye ait birçok oyun olabilir
        public ICollection<Game>? Games { get; set; }
    }
}

namespace EPinAPI.Models
{
    public class GameProductType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int GameId { get; set; }
        public Game Game { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

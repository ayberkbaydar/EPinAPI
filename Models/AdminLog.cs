namespace EPinAPI.Models
{
    public class AdminLog
    {
        public int Id { get; set; }
        public int AdminId { get; set; }
        public string Action { get; set; }
        public string Endpoint { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // İlişkiler
        public User Admin { get; set; }
    }

}

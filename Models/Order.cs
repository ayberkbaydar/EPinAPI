namespace EPinAPI.Models;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; } // Kullanıcı ilişkisi
    public int EpinId { get; set; } // Satın alınan ürün
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending"; // Varsayılan: Pending
    public decimal TotalPrice { get; set; }

    // İlişkiler
    public User? User { get; set; }
    public Epin? Epin { get; set; }
}
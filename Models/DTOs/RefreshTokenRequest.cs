using System.ComponentModel.DataAnnotations;

namespace EPinAPI.Models.DTOs
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; }
    }
}

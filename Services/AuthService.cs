using System;
using System.Security.Cryptography;
using BCrypt.Net;

public static class AuthService
{
    private const int WorkFactor = 12; // Şifre güvenliği için iş faktörü

    // 📌 Şifreyi hashle
    public static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    // 📌 Şifreyi doğrula
    public static bool VerifyPassword(string password, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }

    public static string GenerateRefreshToken()
    {
        var randomNumber = new byte[32]; // 📌 32 byte rastgele token oluştur
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
        }
        return Convert.ToBase64String(randomNumber);
    }
}
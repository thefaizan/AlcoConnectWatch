using System;
using System.Security.Cryptography;
using System.Text;

namespace AlcoConnectWatch.Services
{
    public static class AuthTokenService
    {
        // Secret key for signing tokens - in production, store in config/environment
        private static readonly string SecretKey = "AlcoConnectWatch_SecureKey_2026_!@#$%^&*()_Mining_Compliance";

        // Token expiry in hours
        private const int TokenExpiryHours = 24;

        /// <summary>
        /// Generates a secure signed token for a user
        /// </summary>
        public static string GenerateToken(int userId, string email)
        {
            var expiry = DateTime.UtcNow.AddHours(TokenExpiryHours).Ticks;
            var payload = $"{userId}|{email}|{expiry}";
            var signature = ComputeSignature(payload);

            var token = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{payload}|{signature}")
            );

            return token;
        }

        /// <summary>
        /// Validates a token and returns user info if valid
        /// </summary>
        public static TokenValidationResult ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return new TokenValidationResult { IsValid = false, Error = "Token is empty" };

            try
            {
                // Remove "Bearer " prefix if present
                if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    token = token.Substring(7);

                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                var parts = decoded.Split('|');

                if (parts.Length != 4)
                    return new TokenValidationResult { IsValid = false, Error = "Invalid token format" };

                var userId = int.Parse(parts[0]);
                var email = parts[1];
                var expiry = long.Parse(parts[2]);
                var signature = parts[3];

                // Verify signature
                var payload = $"{userId}|{email}|{expiry}";
                var expectedSignature = ComputeSignature(payload);

                if (signature != expectedSignature)
                    return new TokenValidationResult { IsValid = false, Error = "Invalid token signature" };

                // Check expiry
                var expiryDate = new DateTime(expiry, DateTimeKind.Utc);
                if (DateTime.UtcNow > expiryDate)
                    return new TokenValidationResult { IsValid = false, Error = "Token has expired" };

                return new TokenValidationResult
                {
                    IsValid = true,
                    UserId = userId,
                    Email = email,
                    ExpiresAt = expiryDate
                };
            }
            catch (Exception ex)
            {
                return new TokenValidationResult { IsValid = false, Error = $"Token validation failed: {ex.Message}" };
            }
        }

        /// <summary>
        /// Computes HMAC-SHA256 signature for the payload
        /// </summary>
        private static string ComputeSignature(string payload)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Generates a random salt for password hashing
        /// </summary>
        public static string GenerateSalt()
        {
            var saltBytes = new byte[32];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        /// <summary>
        /// Hashes a password with salt using SHA256
        /// </summary>
        public static string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var combined = password + salt;
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                return Convert.ToBase64String(bytes);
            }
        }

        /// <summary>
        /// Verifies a password against a hash and salt
        /// </summary>
        public static bool VerifyPassword(string password, string storedHash, string salt)
        {
            var computedHash = HashPassword(password, salt);
            return computedHash == storedHash;
        }
    }

    public class TokenValidationResult
    {
        public bool IsValid { get; set; }
        public int UserId { get; set; }
        public string Email { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Error { get; set; }
    }
}

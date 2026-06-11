// Purpose: Password hashing, verification, legacy demo-account compatibility,
// and password-strength validation for authentication workflows.
using System;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;

namespace MindWell
{
    /// <summary>
    /// PasswordHelper provides secure password hashing and verification using PBKDF2 with SHA-256.
    /// NEVER use MD5, SHA1, or plain-text passwords.
    /// </summary>
    public class PasswordHelper
    {
        private const int SaltSize = 16;        // 128 bits
        private const int HashSize = 20;        // 160 bits
        private const int Iterations = 10000;   // PBKDF2 iterations

        /// <summary>
        /// Hashes a password using PBKDF2 (Password-Based Key Derivation Function 2) with cryptographic salt.
        /// </summary>
        /// <param name="password">Plain-text password to hash</param>
        /// <returns>Base64-encoded hash with salt</returns>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be null or empty");

            using (var salt = new RNGCryptoServiceProvider())
            {
                byte[] saltBytes = new byte[SaltSize];
                salt.GetBytes(saltBytes);  // Cryptographically secure random salt

                using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256))
                {
                    byte[] hash = pbkdf2.GetBytes(HashSize);
                    
                    // Combine salt and hash for storage
                    byte[] hashWithSalt = new byte[SaltSize + HashSize];
                    Array.Copy(saltBytes, 0, hashWithSalt, 0, SaltSize);
                    Array.Copy(hash, 0, hashWithSalt, SaltSize, HashSize);
                    
                    // Return as Base64 string for database storage
                    return Convert.ToBase64String(hashWithSalt);
                }
            }
        }

        /// <summary>
        /// Verifies a plain-text password against a stored hash.
        /// </summary>
        /// <param name="password">Plain-text password to verify</param>
        /// <param name="hash">Stored Base64-encoded hash (from HashPassword)</param>
        /// <returns>True if password matches hash, false otherwise</returns>
        public static bool VerifyPassword(string password, string hash)
        {
            // Verifies modern PBKDF2 hashes first, then falls back to legacy seed hashes for lecturer test accounts.
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
                return false;

            try
            {
                byte[] hashWithSalt = Convert.FromBase64String(hash);
                
                if (hashWithSalt.Length != (SaltSize + HashSize))
                    return false;

                byte[] saltBytes = new byte[SaltSize];
                Array.Copy(hashWithSalt, 0, saltBytes, 0, SaltSize);

                using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256))
                {
                    byte[] computedHash = pbkdf2.GetBytes(HashSize);
                    
                    // Compare computed hash with stored hash (constant-time comparison)
                    return ConstantTimeEquals(computedHash, hashWithSalt, SaltSize);
                }
            }
            catch
            {
                return VerifyLegacySha256SeedPassword(password, hash);
            }
        }

        private static bool VerifyLegacySha256SeedPassword(string password, string hash)
        {
            // Existing seed SQL stores the provided demo accounts as SHA-256 hex.
            // New registrations still use PBKDF2; this keeps lecturer test accounts usable.
            if (hash == null || hash.Length != 64 || !Regex.IsMatch(hash, "^[0-9A-Fa-f]{64}$"))
                return false;

            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes)
                    builder.Append(b.ToString("X2"));

                return string.Equals(builder.ToString(), hash, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Constant-time comparison to prevent timing attacks.
        /// </summary>
        private static bool ConstantTimeEquals(byte[] computed, byte[] stored, int offset)
        {
            int mismatch = 0;
            for (int i = 0; i < computed.Length; i++)
            {
                mismatch |= computed[i] ^ stored[i + offset];
            }
            return mismatch == 0;
        }

        /// <summary>
        /// Validates password strength requirements.
        /// </summary>
        /// <param name="password">Password to validate</param>
        /// <returns>True if password meets requirements</returns>
        public static bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            // Minimum 8 characters
            if (password.Length < 8)
                return false;

            bool hasUpperCase = System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Z]");
            bool hasLowerCase = System.Text.RegularExpressions.Regex.IsMatch(password, @"[a-z]");
            bool hasDigit = System.Text.RegularExpressions.Regex.IsMatch(password, @"[0-9]");
            bool hasSpecialChar = System.Text.RegularExpressions.Regex.IsMatch(password, @"[!@#$%^&*]");

            return hasUpperCase && hasLowerCase && hasDigit && hasSpecialChar;
        }

        /// <summary>
        /// Gets password requirement message for display to users.
        /// </summary>
        /// <returns>Password requirement string</returns>
        public static string GetPasswordRequirements()
        {
            return "Password must be at least 8 characters with uppercase, lowercase, number, and special character (!@#$%^&*)";
        }
    }
}

namespace FYP.Services
{
    using System;
    using System.Security.Cryptography;
    using System.Text;

    public class PasswordHasherService
    {
        private const int SaltSize = 16; // 128 bit
        private const int HashSize = 32; // 256 bit
        private const int Iterations = 10000;

        // Hashes the password
        public string HashPassword(string password)
        {
            using (var hmac = new HMACSHA256())
            {
                byte[] salt = new byte[SaltSize];
                using (var rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(salt);
                }

                hmac.Key = salt;
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

                byte[] hashBytes = new byte[SaltSize + HashSize];
                Buffer.BlockCopy(salt, 0, hashBytes, 0, SaltSize);
                Buffer.BlockCopy(hash, 0, hashBytes, SaltSize, HashSize);

                return Convert.ToBase64String(hashBytes);
            }
        }

        // Verifies the password
        public bool VerifyPassword(string password, string storedHash)
        {
            byte[] hashBytes = Convert.FromBase64String(storedHash);
            byte[] salt = new byte[SaltSize];
            Buffer.BlockCopy(hashBytes, 0, salt, 0, SaltSize);

            using (var hmac = new HMACSHA256())
            {
                hmac.Key = salt;
                byte[] computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

                for (int i = 0; i < HashSize; i++)
                {
                    if (hashBytes[i + SaltSize] != computedHash[i])
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}

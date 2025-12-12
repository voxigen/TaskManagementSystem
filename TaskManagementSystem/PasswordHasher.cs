using System;
using System.Security.Cryptography;
using System.Text;

namespace TaskManagementSystem
{
    public static class PasswordHasher
    {
        public static string ComputeHash(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);
                StringBuilder builder = new StringBuilder();
                foreach (byte b in hash)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            string hashedInput = ComputeHash(password);
            return hashedInput.Equals(hashedPassword, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsSimplePassword(string password)
        {
     
            return password.Length < 6 ||
                   password.ToLower() == "password" ||
                   password.ToLower() == "123456" ||
                   password.ToLower() == "qwerty";
        }
    }
}
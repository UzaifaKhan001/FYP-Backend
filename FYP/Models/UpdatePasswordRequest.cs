namespace FYP.Models
{
    public class UpdatePasswordRequest
    {
        public int Id { get; set; } // Changed from string to int
        public string Email { get; set; }
        public string Name { get; set; }
        public string PasswordHash { get; set; } // Plain text old password for verification
        public string NewPassword { get; set; } // Plain text new password
    }
}
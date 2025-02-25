namespace FYP.Models
{
    public class UpdatePasswordRequest
    {
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string NewPassword { get; set; }
    }
}
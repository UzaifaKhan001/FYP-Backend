namespace FYP.Models
{
    public class RegisterRequest
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
    }
}

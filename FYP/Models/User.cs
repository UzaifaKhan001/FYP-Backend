namespace FYP.Models
    {
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ResetToken { get; set; }
        public DateTime? ResetTokenExpiration { get; set; }
    }

}
namespace FYP.Models
{
    public class RegisterRequest
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string BusinessType { get; set; }  // Optional, if you plan to save it
    }
}

namespace FYP.Models
{
    public class UserSettings
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public bool EmailNotifications { get; set; }
        public bool PushNotifications { get; set; }
        public bool Updates { get; set; }
        public string? ProfileVisibility { get; set; }
        public bool ActivityStatus { get; set; }
        public bool EnableSound { get; set; }
        public int Volume { get; set; }
    }
}

namespace FYP.Models
{
    public class UserProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Address { get; set; }
    public string Phone { get; set; }
    public DateTime CreatedAt { get; set; }
}
}
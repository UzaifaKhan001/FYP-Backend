namespace FYP.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Message { get; set; }
        public bool Read { get; set; }  // Boolean type for 'Read'
        public DateTime CreatedAt { get; set; }  // DateTime type for 'CreatedAt'
    }

}

namespace FYP.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int RestaurantId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime DateAdded { get; set; }

        public Restaurant Restaurant { get; set; } // Navigation property to the restaurant
    }

}

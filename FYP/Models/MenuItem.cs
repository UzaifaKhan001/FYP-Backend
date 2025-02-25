namespace FYP.Models
{
    public class MenuItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Picture { get; set; } // Base64 string or URL
        public decimal Price { get; set; }
        public int RestaurantId { get; set; }
    }
}

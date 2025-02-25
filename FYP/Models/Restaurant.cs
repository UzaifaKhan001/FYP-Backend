
namespace FYP.Models
{
    public class Restaurant
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }

        public List<Review> Reviews { get; set; } // Navigation property for reviews
    }

}

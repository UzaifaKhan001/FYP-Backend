namespace Restaurants.Models
{
    public class Restaurant
    {
        public int Id { get; set; }
        public string StoreAddress { get; set; }
        public string? FloorSuite { get; set; }
        public string StoreName { get; set; }
        public string BrandName { get; set; }
        public int BusinessTypeId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public bool AgreedToPrivacy { get; set; }
    }

}

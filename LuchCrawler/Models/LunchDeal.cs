namespace LunchCrawler.Models
{
    public class LunchDeal : MongoModelBase
    {
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string RestaurantId { get; set; }
        public Restaurant Restaurant { get; set; }
        public string time { get; set; }
    }
}
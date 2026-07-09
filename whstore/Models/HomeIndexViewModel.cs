namespace whstore.Models
{
    public class HomeIndexViewModel
    {
        public IEnumerable<Product> TrendingProducts { get; set; } = new List<Product>();
        public IEnumerable<Product> AliExpressProducts { get; set; } = new List<Product>();
        public string? CurrentFilter { get; set; }
    }
}

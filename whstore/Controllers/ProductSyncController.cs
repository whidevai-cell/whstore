namespace whstore.Models
{
    public class ProductDTO
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Price { get; set; }
        public string? OriginalPrice { get; set; }
        public string? ImageUrl { get; set; }
        public string? AffiliateLink { get; set; }
        public string? CommissionRate { get; set; }
        public string? ShippingCost { get; set; }
        public string? StoreName { get; set; }
        public string? Category { get; set; }
        public string? ReviewCount { get; set; }
        public string? ReviewRate { get; set; }
        public string? Attributes { get; set; }
        public bool IsHotProduct { get; set; }
    }
}
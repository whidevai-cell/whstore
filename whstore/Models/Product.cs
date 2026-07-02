using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace whstore.Models
{
    [BsonIgnoreExtraElements] // ডাটাবেজে যদি অতিরিক্ত ফিল্ড থাকে তবে এরর দেবে না
    public class Product
    {
        [BsonId]
        public object? Id { get; set; }

        // আপনার কাঙ্ক্ষিত নতুন ফিল্ড
        [BsonElement("productId")]
        public string? ProductId { get; set; }

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;
        [BsonElement("description")]
        public string? Description { get; set; }
        [BsonElement("price")]
        public string? Price { get; set; }
        [BsonElement("originalPrice")]
        public string? OriginalPrice { get; set; }
        [BsonElement("imageUrl")]
        public string? ImageUrl { get; set; }
        [BsonElement("affiliateLink")]
        public string? AffiliateLink { get; set; }
        [BsonElement("commissionRate")]
        public string? CommissionRate { get; set; }
        [BsonElement("shippingCost")]
        public string? ShippingCost { get; set; }
        [BsonElement("storeName")]
        public string? StoreName { get; set; }
        [BsonElement("category")]
        public string? Category { get; set; }
        [BsonElement("reviewCount")]
        public int ReviewCount { get; set; }
        [BsonElement("reviewRate")]
        public string? ReviewRate { get; set; }
        [BsonElement("attributes")]
        public string? Attributes { get; set; }
        [BsonElement("isHotProduct")]
        public bool IsHotProduct { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; }

        [BsonElement("lastUpdated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
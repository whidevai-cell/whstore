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
        public string? ProductId { get; set; }

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
        public int ReviewCount { get; set; }
        public string? ReviewRate { get; set; }
        public string? Attributes { get; set; }
        public bool IsHotProduct { get; set; }

        public bool IsActive { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
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
        [BsonElement("ProductId")]
        public string? ProductId { get; set; }

        [BsonElement("Title")]
        public string Title { get; set; } = string.Empty;
        [BsonElement("Description")]
        public string? Description { get; set; }
        [BsonElement("Price")]
        public string? Price { get; set; }
        [BsonElement("OriginalPrice")]
        public string? OriginalPrice { get; set; }
        [BsonElement("ImageUrl")]
        public string? ImageUrl { get; set; }
        [BsonElement("AffiliateLink")]
        public string? AffiliateLink { get; set; }
        [BsonElement("CommissionRate")]
        public string? CommissionRate { get; set; }
        [BsonElement("ShippingCost")]
        public string? ShippingCost { get; set; }
        [BsonElement("StoreName")]
        public string? StoreName { get; set; }
        [BsonElement("Category")]
        public string? Category { get; set; }
        [BsonElement("ReviewCount")]
        public int ReviewCount { get; set; }
        [BsonElement("ReviewRate")]
        public string? ReviewRate { get; set; }
        [BsonElement("Attributes")]
        public string? Attributes { get; set; }
        [BsonElement("IsHotProduct")]
        public bool IsHotProduct { get; set; }

        [BsonElement("IsActive")]
        public bool IsActive { get; set; }

        [BsonElement("LastUpdated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
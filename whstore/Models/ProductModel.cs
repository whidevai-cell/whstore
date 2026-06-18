using Google.GenAI;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace whstore.Models
{
    [Table("Products")]
    [BsonIgnoreExtraElements] // <-- এই লাইনটি অবশ্যই যোগ করুন
    public class Product
    {
        [Key]
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("ProductId")]
        [NotMapped]
        public string? ProductId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Category { get; set; }

        public string? Price { get; set; }
        public string? OriginalPrice { get; set; }
        public string? CommissionRate { get; set; }
        public string? ShippingCost { get; set; }

        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public string? AffiliateLink { get; set; }
        public string? ProductUrl { get; set; }
        public string? StoreName { get; set; }

        [Column("ReviewCount")]
        public int ReviewCount { get; set; }

        public string? ReviewRate { get; set; }
        public string? Attributes { get; set; }

        [Column("IsHotProduct")]
        public bool IsHotProduct { get; set; }

        [Column("IsActive")]
        public bool IsActive { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
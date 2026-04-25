using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace whstore.Models
{
    [Table("products")] // PostgreSQL টেবিল
    public class ProductModel
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("productid")]
        public string? ProductId { get; set; }

        [Column("title")]
        public string? Title { get; set; }

        [Column("producturl")]
        public string? ProductUrl { get; set; }

        [Column("affiliatelink")]
        public string? AffiliateLink { get; set; }

        [Column("imageurl")]
        public string? ImageUrl { get; set; }

        [Column("price")]
        public string? Price { get; set; }

        [Column("originalprice")]
        public string? OriginalPrice { get; set; }

        [Column("commissionrate")]
        public string? CommissionRate { get; set; }

        [Column("shippingcost")]
        public string? ShippingCost { get; set; }

        [Column("storename")]
        public string? StoreName { get; set; }

        [Column("category")]
        public string? Category { get; set; }

        // এরর দূর করার জন্য এই ৩টি প্রপার্টি যোগ করা হয়েছে
        [Column("reviewcount")]
        public string? ReviewCount { get; set; }

        [Column("reviewrate")]
        public string? ReviewRate { get; set; }

        [Column("attributes")]
        public string? Attributes { get; set; }

        [Column("ishotproduct")]
        public bool IsHotProduct { get; set; } = false;

        [Column("isactive")]
        public bool IsActive { get; set; } = true;

        [Column("lastupdated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    // এটি শুধু ড্যাশবোর্ড থেকে ডাটা রিসিভ করার জন্য (API Helper)
    public class ProductSyncModel
    {
        public string? title { get; set; }
        public string? price { get; set; }
        public string? originalprice { get; set; }
        public string? imageurl { get; set; }
        public string? affiliatelink { get; set; }
        public string? commissionrate { get; set; }
        public string? shippingcost { get; set; }
        public string? storename { get; set; }
        public string? category { get; set; }
        public string? reviewcount { get; set; }
        public string? reviewrate { get; set; }
        public string? attributes { get; set; }
        public bool ishotproduct { get; set; }
    }
}
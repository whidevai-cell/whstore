using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace whstore.Models
{
    public class ProductModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = string.Empty;

        public string? Category { get; set; }

        [Required(ErrorMessage = "Price is required")]
        public string Price { get; set; } = "0";

        public string? OriginalPrice { get; set; }

        public string? ImageUrl { get; set; }

        public string? Description { get; set; }

        public string? AffiliateLink { get; set; }

        public string? ProductUrl { get; set; }

        public string? CommissionRate { get; set; }

        public string? ShippingCost { get; set; }

        public string? StoreName { get; set; }

        public int ReviewCount { get; set; }

        public string? ReviewRate { get; set; }

        public string? Attributes { get; set; }

        public bool IsHotProduct { get; set; }

        public bool IsActive { get; set; } = true;

        // SQLite-এ datetime2 বা GETDATE() এর সমস্যা এড়াতে ডিফল্ট ভ্যালু কোড থেকেই সেট করা হয়েছে
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace whstore.Models
{
    public class ProductModel
    {
        [Key]
        public int Id { get; set; } // Database Primary Key

        [JsonPropertyName("productid")]
        public string? ProductId { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("producturl")]
        public string? ProductUrl { get; set; } // অরিজিনাল প্রোডাক্ট লিঙ্ক

        [JsonPropertyName("promotionurl")]
        public string? AffiliateLink { get; set; } // আপনার অ্যাফিলিয়েট লিঙ্ক

        [JsonPropertyName("imageurl")]
        public string? ImageUrl { get; set; } // এখানে সরাসরি ছবির ডাটা (Base64) সেভ হবে

        [JsonPropertyName("price")]
        public string? Price { get; set; } // বর্তমান দাম

        [JsonPropertyName("originalprice")]
        public string? OriginalPrice { get; set; } // আগের বা আসল দাম

        [JsonPropertyName("commissionrate")]
        public string? CommissionRate { get; set; } // কত % কমিশন পাবেন

        [JsonPropertyName("evaluationcount")]
        public string? ReviewCount { get; set; } // মোট রিভিউ সংখ্যা

        [JsonPropertyName("goodreviewrate")]
        public string? ReviewRate { get; set; } // ইতিবাচক রিভিউর হার (%)

        [JsonPropertyName("shippingcost")]
        public string? ShippingCost { get; set; } // শিপিং খরচ (যেমন: 0 বা Free)

        [JsonPropertyName("storename")]
        public string? StoreName { get; set; } // দোকানের নাম

        [JsonPropertyName("categoryid")]
        public string? Category { get; set; } // ক্যাটেগরি

        [JsonPropertyName("attributes")]
        public string? Attributes { get; set; } // কালার, সাইজ ইত্যাদি

        [JsonPropertyName("hotproduct")]
        public bool IsHotProduct { get; set; } = false; // জনপ্রিয় পণ্য কি না

        public string? Source { get; set; } = "Local-Upload"; // সোর্স ট্যাগ

        public bool IsActive { get; set; } = true;

        // টাইমজোন সমস্যার জন্য UtcNow ব্যবহার করা হয়েছে
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
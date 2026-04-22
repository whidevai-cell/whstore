using Microsoft.AspNetCore.Mvc;
using whstore.Models;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace whstore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductSyncController : ControllerBase
    {
        private readonly string? _cloudConn;

        public ProductSyncController(IConfiguration configuration)
        {
            // Render ড্যাশবোর্ড থেকে ConnectionStrings:DefaultConnection নেওয়া হচ্ছে
            _cloudConn = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpPost("sync-built-in")]
        public async Task<IActionResult> SyncFromInternalDashboard([FromBody] ProductModel product)
        {
            // ১. ডাটা ভ্যালিডেশন
            if (product == null || string.IsNullOrWhiteSpace(product.Title))
                return BadRequest(new { success = false, message = "প্রোডাক্টের নাম (Title) ছাড়া সেভ করা সম্ভব নয়!" });

            if (string.IsNullOrEmpty(_cloudConn))
                return StatusCode(500, new { success = false, message = "ডাটাবেস কানেকশন স্ট্রিং পাওয়া যায়নি!" });

            try
            {
                using (var conn = new NpgsqlConnection(_cloudConn))
                {
                    await conn.OpenAsync();

                    // ২. PostgreSQL এর জন্য Upsert লজিক (Title ইউনিক হতে হবে)
                    string sql = @"
                        INSERT INTO products (
                            title, price, originalprice, imageurl, affiliatelink, 
                            commissionrate, shippingcost, storename, hotproduct, 
                            isactive, lastupdated
                        ) 
                        VALUES (
                            @title, @price, @oPrice, @img, @link, 
                            @comm, @ship, @store, @hot, true, @updated
                        )
                        ON CONFLICT (title) DO UPDATE SET 
                            price = EXCLUDED.price,
                            imageurl = EXCLUDED.imageurl,
                            affiliatelink = EXCLUDED.affiliatelink,
                            lastupdated = EXCLUDED.lastupdated,
                            hotproduct = EXCLUDED.hotproduct;";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        // প্যারামিটারগুলো সঠিকভাবে ম্যাপ করা
                        cmd.Parameters.AddWithValue("title", (object?)product.Title ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("price", (object?)product.Price ?? "0");
                        cmd.Parameters.AddWithValue("oPrice", (object?)product.OriginalPrice ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("img", (object?)product.ImageUrl ?? "");
                        cmd.Parameters.AddWithValue("link", (object?)product.AffiliateLink ?? "#");
                        cmd.Parameters.AddWithValue("comm", (object?)product.CommissionRate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("ship", (object?)product.ShippingCost ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("store", (object?)product.StoreName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("hot", product.IsHotProduct);
                        cmd.Parameters.AddWithValue("updated", DateTime.UtcNow);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                // ৩. সফল রেসপন্স
                return Ok(new
                {
                    success = true,
                    message = "Saved to Cloud Successfully!",
                    timestamp = DateTime.UtcNow.AddHours(6).ToString("hh:mm tt")
                });
            }
            catch (Exception ex)
            {
                // এরর লগিং এবং মেসেজ পাঠানো
                Console.WriteLine($"[Sync Error]: {ex.Message}");
                return StatusCode(500, new { success = false, error = "Cloud Sync Error: " + ex.Message });
            }
        }
    }
}

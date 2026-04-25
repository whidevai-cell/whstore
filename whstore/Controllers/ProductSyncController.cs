using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Threading.Tasks;
using whstore.Models;

namespace whstore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductSyncController : ControllerBase
    {
        private readonly string? _cloudConn;
        private readonly ApplicationDbContext _context;

        public ProductSyncController(IConfiguration configuration, ApplicationDbContext context)
        {
            // appsettings.json থেকে PostgreSQL কানেকশন নেওয়া হচ্ছে
            _cloudConn = configuration.GetConnectionString("DefaultConnection");
            _context = context;
        }

        [HttpPost("sync-built-in")]
        public async Task<IActionResult> SyncFromInternalDashboard([FromBody] ProductSyncModel incomingProduct)
        {
            if (incomingProduct == null)
                return BadRequest(new { success = false, message = "ড্যাশবোর্ড থেকে কোনো ডাটা পাওয়া যায়নি!" });

            try
            {
                // ১. ড্যাশবোর্ড থেকে আসা ডাটাকে মেইন মডেলে ম্যাপ করা
                var product = new ProductModel
                {
                    Title = incomingProduct.title ?? "No Title",
                    Price = incomingProduct.price ?? "0",
                    OriginalPrice = incomingProduct.originalprice ?? "0",
                    ImageUrl = incomingProduct.imageurl ?? "",
                    AffiliateLink = incomingProduct.affiliatelink ?? "#",
                    CommissionRate = incomingProduct.commissionrate ?? "0",
                    ShippingCost = incomingProduct.shippingcost ?? "Free",
                    StoreName = incomingProduct.storename ?? "Global",
                    Category = incomingProduct.category ?? "General",
                    ReviewCount = incomingProduct.reviewcount ?? "0",
                    ReviewRate = incomingProduct.reviewrate ?? "0",
                    Attributes = incomingProduct.attributes ?? "",
                    IsHotProduct = incomingProduct.ishotproduct,
                    IsActive = true,
                    LastUpdated = DateTime.UtcNow
                };

                // ২. PostgreSQL এ সেভ করা (Entity Framework ব্যবহার করে সহজভাবে)
                // টাইটেল দিয়ে চেক করা হচ্ছে প্রোডাক্টটি আগে থেকেই আছে কি না
                var existingProduct = await _context.Products
                    .FirstOrDefaultAsync(p => p.Title == product.Title);

                if (existingProduct != null)
                {
                    // আপডেট করা
                    existingProduct.Price = product.Price;
                    existingProduct.ImageUrl = product.ImageUrl;
                    existingProduct.LastUpdated = DateTime.UtcNow;
                    _context.Products.Update(existingProduct);
                }
                else
                {
                    // নতুন প্রোডাক্ট যোগ করা
                    await _context.Products.AddAsync(product);
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Saved to PostgreSQL Successfully!",
                    timestamp = DateTime.UtcNow.AddHours(6).ToString("hh:mm tt")
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Database Save Error: " + ex.Message
                });
            }
        }
    }
}
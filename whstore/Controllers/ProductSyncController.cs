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
            _cloudConn = configuration.GetConnectionString("DefaultConnection");
            _context = context;
        }

        [HttpPost("sync-built-in")]
        public async Task<IActionResult> SyncFromInternalDashboard([FromBody] ProductModel incomingProduct)
        {
            if (incomingProduct == null)
                return BadRequest(new { success = false, message = "ড্যাশবোর্ড থেকে কোনো ডাটা পাওয়া যায়নি!" });

            try
            {
                // ১. ড্যাশবোর্ড থেকে আসা ডাটাকে মেইন মডেলে ম্যাপ করা (নামগুলো বড় হাতের করা হয়েছে)
                var product = new ProductModel
                {
                    Title = incomingProduct.Title ?? "No Title",
                    Price = incomingProduct.Price ?? "0",
                    OriginalPrice = incomingProduct.OriginalPrice ?? "0",
                    ImageUrl = incomingProduct.ImageUrl ?? "",
                    AffiliateLink = incomingProduct.AffiliateLink ?? "#",
                    CommissionRate = incomingProduct.CommissionRate ?? "0",
                    ShippingCost = incomingProduct.ShippingCost ?? "Free",
                    StoreName = incomingProduct.StoreName ?? "Global",
                    Category = incomingProduct.Category ?? "General",
                    ReviewCount = incomingProduct.ReviewCount ?? "0",
                    ReviewRate = incomingProduct.ReviewRate ?? "0",
                    Attributes = incomingProduct.Attributes ?? "",
                    IsHotProduct = incomingProduct.IsHotProduct,
                    IsActive = true,
                    LastUpdated = DateTime.UtcNow
                };

                // ২. টাইটেল দিয়ে চেক করা হচ্ছে প্রোডাক্টটি আগে থেকেই আছে কি না
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
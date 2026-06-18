using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using whstore.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace whstore.Controllers
{
    public class ProductSyncController : Controller
    {
        private readonly IMongoCollection<Product> _mongoCollection;

        public ProductSyncController(IMongoDatabase database)
        {
            _mongoCollection = database.GetCollection<Product>("Products");
        }

        public async Task<IActionResult> SyncProducts()
        {
            // ১. এখানে await ব্যবহার করা হয়েছে যাতে asynchronous ডাটা পাওয়া যায়
            var dtos = await GetProductsFromSource();

            if (dtos == null || dtos.Count == 0)
            {
                return Ok("No products to sync.");
            }

            foreach (var dto in dtos)
            {
                var product = new Product
                {
                    Title = dto.Title ?? "Untitled",
                    Description = dto.Description,
                    ImageUrl = dto.ImageUrl,
                    AffiliateLink = dto.AffiliateLink,
                    StoreName = dto.StoreName,
                    Category = dto.Category,
                    Attributes = dto.Attributes,
                    IsHotProduct = dto.IsHotProduct,

                    // ফরম্যাটিং ঠিক করা হয়েছে
                    Price = dto.Price?.Replace("BDT", "").Replace(",", "").Trim(),
                    OriginalPrice = dto.OriginalPrice?.Replace("BDT", "").Replace(",", "").Trim(),
                    CommissionRate = dto.CommissionRate?.Replace("%", "").Trim(),
                    ShippingCost = dto.ShippingCost?.Replace("BDT", "").Replace(",", "").Trim(),

                    ReviewCount = int.TryParse(dto.ReviewCount, out var rc) ? rc : 0,
                    ReviewRate = dto.ReviewRate,
                    LastUpdated = DateTime.UtcNow
                };

                await _mongoCollection.InsertOneAsync(product);
            }
            return Ok("Sync Complete");
        }

        // ২. রিটার্ন টাইপ Task<List<ProductDTO>> নিশ্চিত করা হয়েছে
        private async Task<List<ProductDTO>> GetProductsFromSource()
        {
            // এখানে আপনার ডাটা সোর্স থেকে ডাটা আনার লজিক বসান
            return await Task.FromResult(new List<ProductDTO>());
        }
    }
}
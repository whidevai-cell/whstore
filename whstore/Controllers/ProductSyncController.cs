using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver; // এটি নিশ্চিত করুন
using whstore.Models;
using whstore.Services;

public class ProductSyncController : Controller
{
    private readonly IProductRepository _repository; // আপনার রিপোজিটরি ইন্টারফেস
    private readonly IMongoDatabase _database;       // ডাটাবেজ অবজেক্ট

    // কনস্ট্রাক্টর ইনজেকশন
    public ProductSyncController(IProductRepository repository, IMongoDatabase database)
    {
        _repository = repository;
        _database = database;
    }

    public async Task<IActionResult> Sync()
    {
        // ১. ডাটা সোর্স থেকে dtos আনুন
        var dtos = await _repository.GetAllAsync();

        // ২. কালেকশন ডিফাইন করুন
        var _mongoCollection = _database.GetCollection<Product>("products");

        // ৩. লুপ চালিয়ে ডাটা ম্যাপ ও ইনসার্ট করুন
        if (dtos != null)
        {
            foreach (var dto in dtos)
            {
                var product = new Product
                {
                    Title = dto.Title ?? "Untitled",
                    Description = dto.Description ?? string.Empty,
                    ImageUrl = dto.ImageUrl ?? string.Empty,
                    AffiliateLink = dto.AffiliateLink ?? string.Empty,
                    StoreName = dto.StoreName ?? string.Empty,
                    Category = dto.Category ?? string.Empty,

                    // Attributes DTO-তে string, Product মডেলে object
                    Attributes = dto.Attributes,

                    IsHotProduct = dto.IsHotProduct,
                    IsActive = true,

                    Price = dto.Price ?? "0",
                    OriginalPrice = dto.OriginalPrice ?? "0",
                    CommissionRate = dto.CommissionRate ?? "0",
                    ShippingCost = dto.ShippingCost ?? "0",

                    ReviewCount = dto.ReviewCount,
                    ReviewRate = dto.ReviewRate ?? "0",
                    LastUpdated = DateTime.UtcNow
                };

                await _mongoCollection.InsertOneAsync(product);
            }
        }

        return Ok("Sync Completed Successfully!");
    }
}
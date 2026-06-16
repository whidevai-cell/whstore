using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using whstore.Data;
using whstore.Models;
using MongoDB.Driver;

namespace whstore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IMongoCollection<Product> _mongoCollection;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IMongoClient mongoClient)
        {
            _logger = logger;
            _context = context;
            var database = mongoClient.GetDatabase("WhStoreDb");
            _mongoCollection = database.GetCollection<Product>("AffiliateProducts");
        }

        public async Task<IActionResult> Index(string? searchString)
        {
            // PostgreSQL থেকে ডাটা আনার সময় সরাসরি Id দিয়ে অর্ডার না করে বা ফিল্টার না করে 
            // ডাটা নিয়ে এসে তারপর মেমোরিতে সাজানো বেশি নিরাপদ।
            var neonProducts = await _context.Products.Where(p => p.IsActive).ToListAsync();
            var mongoProducts = await _mongoCollection.Find(_ => true).ToListAsync();

            var allProducts = new List<Product>();
            allProducts.AddRange(neonProducts);
            allProducts.AddRange(mongoProducts);

            if (!string.IsNullOrEmpty(searchString))
            {
                allProducts = allProducts.Where(p => (p.Title ?? "").Contains(searchString, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // এখানে Id এর বদলে LastUpdated দিয়ে সর্টিং করুন, কারণ Id স্ট্রিং হলে সর্টিংয়ে সমস্যা হতে পারে
            return View(allProducts.OrderByDescending(p => p.LastUpdated).ToList());
        }

        public async Task<IActionResult> Privacy()
        {
            var products = await _context.Products.OrderByDescending(p => p.LastUpdated).ToListAsync();
            return View(products);
        }

        // ... (Embed এবং অন্যান্য মেথড ঠিক আছে)

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(string id)
        {
            // FindAsync এর জায়গায় FirstOrDefaultAsync ব্যবহার করুন যা ডাটাবেস এরর কমায়
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return Redirect("/whidestore");
        }

        // অন্যান্য মেথডগুলো একই থাকবে...
    }
}
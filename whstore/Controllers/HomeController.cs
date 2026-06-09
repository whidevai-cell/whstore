using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using whstore.Data;
using whstore.Models;
using whstore.Services;

namespace whstore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly GoogleDriveService _driveService;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, GoogleDriveService driveService)
        {
            _logger = logger;
            _context = context;
            _driveService = driveService;
        }

        // মূল হোম পেজ
        public async Task<IActionResult> Index(string searchString)
        {
            var products = _context.Products.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Title.Contains(searchString) || p.Category.Contains(searchString));
                ViewData["CurrentFilter"] = searchString;
            }
            return View(await products.OrderByDescending(p => p.Id).ToListAsync());
        }

        // Privacy মেথড - স্থায়ীভাবে যোগ করা হলো
        public async Task<IActionResult> Privacy()
        {
            var products = await _context.Products.OrderByDescending(p => p.Id).ToListAsync();
            return View("UserDashboard", products);
        }

        // Embed মেথড - স্থায়ীভাবে যোগ করা হলো
        public async Task<IActionResult> Embed()
        {
            var products = await _context.Products.OrderByDescending(p => p.Id).ToListAsync();
            return View("Embed", products);
        }

        [HttpPost]
        public async Task<IActionResult> SaveProduct(ProductModel product, IFormFile? imageFile)
        {
            try
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    using (var stream = imageFile.OpenReadStream())
                    {
                        var fileId = await _driveService.UploadFileAsync(stream, imageFile.FileName, imageFile.ContentType);
                        product.ImageUrl = $"https://lh3.googleusercontent.com/d/{fileId}";
                    }
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = "✅ Product saved successfully!";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Upload Error: {ex.Message}");
                TempData["Error"] = "❌ Save failed: " + ex.Message;
            }
            return Redirect("/whidestore");
        }

        [Route("whidestore")]
        public async Task<IActionResult> SecretDashboard()
        {
            var products = await _context.Products.OrderByDescending(p => p.Id).ToListAsync();
            return View("UserDashboard", products);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProduct(ProductModel product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
            return Redirect("/whidestore");
        }

        [HttpPost]
        [Route("whidestore/delete/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return Redirect("/whidestore");
        }
    }
}
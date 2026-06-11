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
        public async Task<IActionResult> Index(string? searchString)
        {
            var products = _context.Products.Where(p => p.IsActive).AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => (p.Title ?? string.Empty).Contains(searchString) || (p.Category ?? string.Empty).Contains(searchString));
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

        // Embed মেথড - video/embed hub
        [HttpGet("Embed")]
        public async Task<IActionResult> Embed()
        {
            var embeds = await _context.Embeds.OrderByDescending(e => e.CreatedAt).ToListAsync();
            return View("Embed", embeds);
        }

        [HttpPost]
        public async Task<IActionResult> Create(EmbedModel embed)
        {
            if (string.IsNullOrWhiteSpace(embed.EmbedUrl))
            {
                TempData["Error"] = "Embed URL cannot be empty.";
                return RedirectToAction("Embed");
            }

            if (string.IsNullOrWhiteSpace(embed.Title))
            {
                embed.Title = GetEmbedTitle(embed.EmbedUrl);
            }

            embed.CreatedAt = DateTime.UtcNow;
            embed.IsVisible = true;

            _context.Embeds.Add(embed);
            await _context.SaveChangesAsync();
            TempData["Success"] = "✅ Video saved successfully!";
            return RedirectToAction("Embed");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleVisibility(int id)
        {
            var embed = await _context.Embeds.FindAsync(id);
            if (embed != null)
            {
                embed.IsVisible = !embed.IsVisible;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Embed");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var embed = await _context.Embeds.FindAsync(id);
            if (embed != null)
            {
                _context.Embeds.Remove(embed);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Embed");
        }

        private string GetEmbedTitle(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return "YouTube Video";
            }

            if (url.Contains("shorts/"))
            {
                var videoId = url.Split("shorts/")[1].Split("?")[0];
                return $"YouTube Shorts - {videoId}";
            }
            if (url.Contains("watch?v="))
            {
                var videoId = url.Split("watch?v=")[1].Split("&")[0];
                return $"YouTube Video - {videoId}";
            }
            if (url.Contains("youtu.be/"))
            {
                var videoId = url.Split("youtu.be/")[1].Split("?")[0];
                return $"YouTube Video - {videoId}";
            }
            if (url.Contains("embed/"))
            {
                var videoId = url.Split("embed/")[1].Split("?")[0];
                return $"YouTube Video - {videoId}";
            }

            return "YouTube Video";
        }

        [HttpPost]
        public async Task<IActionResult> SaveProduct(ProductModel product, IFormFile? imageFile)
        {
            try
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    if (_driveService.IsInitialized)
                    {
                        using (var stream = imageFile.OpenReadStream())
                        {
                            var fileId = await _driveService.UploadFileAsync(stream, imageFile.FileName, imageFile.ContentType);
                            product.ImageUrl = $"https://drive.google.com/uc?export=view&id={fileId}";
                        }
                    }
                    else
                    {
                        TempData["Warning"] = "⚠️ Google Drive is not configured. Use Image URL or configure GoogleDrive:ServiceAccountJson / service_account.json.";
                    }
                }

                if (string.IsNullOrWhiteSpace(product.ImageUrl))
                {
                    product.ImageUrl = "https://via.placeholder.com/300?text=No+Image";
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                if (TempData["Warning"] != null)
                {
                    TempData["Success"] = "⚠️ Product saved, but Google Drive is not configured. Use Image URL for image uploads.";
                }
                else
                {
                    TempData["Success"] = "✅ Product saved successfully!";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Upload Error: {ex.Message}");
                TempData["Error"] = "❌ Save failed: " + ex.Message;
            }
            return RedirectToAction("Privacy");
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
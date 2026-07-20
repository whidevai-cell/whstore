using Microsoft.AspNetCore.Mvc;
using whstore.Models;
using whstore.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Net.Http;
using System.Net.Http.Json;

namespace whstore.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductRepository _productRepository;

        public HomeController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        // --- Auth Methods ---
        [HttpGet]
        public IActionResult Login()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (result.Succeeded) return RedirectToAction("Income");
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index");
        }

        // --- AI Chatbot Method ---
        [HttpPost]
        public async Task<IActionResult> ChatWithAI([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Message))
                return BadRequest(new { reply = "Message is empty." });

            try
            {
                string aiResponse = await GetResponseFromGroq(request.Message);
                return Json(new { reply = aiResponse });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { reply = "Error: " + ex.Message });
            }
        }

        private async Task<string> GetResponseFromGroq(string userMessage)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "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");

            var body = new
            {
                model = "llama3-8b-8192",
                messages = new[] { new { role = "user", content = userMessage } }
            };

            var response = await client.PostAsJsonAsync("https://api.groq.com/openai/v1/chat/completions", body);
            return await response.Content.ReadAsStringAsync();
        }

        // --- Public Methods ---
        public async Task<IActionResult> Index(string? searchString)
        {
            var allProducts = await _productRepository.GetActiveAsync(searchString ?? "");

            foreach (var product in allProducts)
            {
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    if (product.ImageUrl.Contains("<img") || product.ImageUrl.Contains("src="))
                    {
                        var match = Regex.Match(product.ImageUrl, @"src=[""'](?<url>.*?)[""']");
                        if (match.Success) product.ImageUrl = match.Groups["url"].Value;
                    }

                    product.ImageUrl = product.ImageUrl.Replace("\"", "").Replace("'", "").Trim();

                    if (product.ImageUrl.Contains(".png_")) product.ImageUrl = product.ImageUrl.Split(".png_")[0] + ".png";
                    else if (product.ImageUrl.Contains(".jpg_")) product.ImageUrl = product.ImageUrl.Split(".jpg_")[0] + ".jpg";
                    else if (product.ImageUrl.Contains(".jpeg_")) product.ImageUrl = product.ImageUrl.Split(".jpeg_")[0] + ".jpeg";
                    else if (product.ImageUrl.EndsWith(".avif") && product.ImageUrl.Contains(".png"))
                        product.ImageUrl = product.ImageUrl.Replace("_.avif", "").Replace(".avif", "");

                    if (product.ImageUrl.StartsWith("//")) product.ImageUrl = "https:" + product.ImageUrl;

                    if (!product.ImageUrl.StartsWith("http") && !product.ImageUrl.StartsWith("/"))
                        product.ImageUrl = "/images/default-product.png";
                }
                else product.ImageUrl = "/images/default-product.png";
            }

            var viewModel = new HomeIndexViewModel
            {
                TrendingProducts = allProducts.Where(p => string.IsNullOrEmpty(p.StoreName) || !p.StoreName.Equals("AliExpress", StringComparison.OrdinalIgnoreCase)).ToList(),
                AliExpressProducts = allProducts.Where(p => p.StoreName != null && p.StoreName.Equals("AliExpress", StringComparison.OrdinalIgnoreCase)).ToList(),
                CurrentFilter = searchString
            };
            return View(viewModel);
        }

        // --- Admin Methods ---
        [HttpPost]
        public async Task<IActionResult> UploadProduct(Product product, IFormFile? imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }
                product.ImageUrl = "/images/uploads/" + uniqueFileName;
            }
            else if (string.IsNullOrEmpty(product.ImageUrl)) product.ImageUrl = "/images/default-product.png";

            product.IsActive = true;
            await _productRepository.AddAsync(product);
            TempData["Success"] = "Product uploaded successfully!";
            return RedirectToAction("Privacy");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
        {
            if (product == null || string.IsNullOrEmpty(product.Id?.ToString()))
            {
                TempData["Error"] = "Invalid request.";
                return RedirectToAction("Privacy");
            }

            if (ModelState.IsValid)
            {
                var existingProduct = await _productRepository.GetByIdAsync(product.Id.ToString());
                if (existingProduct == null) return NotFound();

                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "uploads");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }
                    product.ImageUrl = "/images/uploads/" + uniqueFileName;
                }
                else product.ImageUrl = existingProduct.ImageUrl;

                var result = await _productRepository.UpdateAsync(product);
                if (result) TempData["Success"] = "Product updated successfully!";
                else TempData["Error"] = "Failed to update product.";

                return RedirectToAction("Privacy");
            }
            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var product = await _productRepository.GetByIdAsync(id);
            return View(product);
        }

        public async Task<IActionResult> Privacy() => View(await _productRepository.GetProductsForAdminAsync());

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            await _productRepository.DeleteAsync(id);
            return RedirectToAction("Privacy");
        }

        public async Task<IActionResult> Details(string id) => View(new ProductDetailsViewModel { Product = await _productRepository.GetByIdAsync(id ?? "") });

        public IActionResult Video() => View();
        public IActionResult Income() => View(new List<DailyPost>());
    }

    public class ChatRequest { public string Message { get; set; } }
}
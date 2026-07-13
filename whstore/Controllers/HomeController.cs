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

        // --- Public Methods ---
        public async Task<IActionResult> Index(string? searchString)
        {
            var allProducts = await _productRepository.GetActiveAsync(searchString ?? "");

            // 🛠️ ইমেজ ইউআরএল সম্পূর্ণ ক্লিন ও ফিক্স করার অ্যাডভান্সড লজিক
            foreach (var product in allProducts)
            {
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    // ১. যদি HTML ট্যাগ বা src= থেকে থাকে, তবে শুধু ভেতরের URL বের করবে
                    if (product.ImageUrl.Contains("<img") || product.ImageUrl.Contains("src="))
                    {
                        var match = Regex.Match(product.ImageUrl, @"src=[""'](?<url>.*?)[""']");
                        if (match.Success)
                        {
                            product.ImageUrl = match.Groups["url"].Value;
                        }
                    }

                    // ২. কোটেশন ও অপ্রয়োজনীয় হোয়াইটস্পেস রিমুভ করা
                    product.ImageUrl = product.ImageUrl.Replace("\"", "").Replace("'", "").Trim();

                    // ৩. 🌟 AliExpress-এর ডাবল এক্সটেনশন ও নোংরা লেজ (.png_220x220.png_.avif) ফিক্স করা
                    // এটি আপনার লোকাল আপলোড করা ছবির আসল ফাইল ফরম্যাট রিস্টোর করবে
                    if (product.ImageUrl.Contains(".png_"))
                    {
                        product.ImageUrl = product.ImageUrl.Split(".png_")[0] + ".png";
                    }
                    else if (product.ImageUrl.Contains(".jpg_"))
                    {
                        product.ImageUrl = product.ImageUrl.Split(".jpg_")[0] + ".jpg";
                    }
                    else if (product.ImageUrl.Contains(".jpeg_"))
                    {
                        product.ImageUrl = product.ImageUrl.Split(".jpeg_")[0] + ".jpeg";
                    }
                    else if (product.ImageUrl.EndsWith(".avif") && product.ImageUrl.Contains(".png"))
                    {
                        product.ImageUrl = product.ImageUrl.Replace("_.avif", "").Replace(".avif", "");
                    }

                    // ৪. গ্লোবাল সিডিএন স্ল্যাশ ফিক্স
                    if (product.ImageUrl.StartsWith("//"))
                    {
                        product.ImageUrl = "https:" + product.ImageUrl;
                    }

                    // ৫. ভ্যালিডেশন: লিংক যদি http বা লোকাল স্ল্যাশ দিয়ে শুরু না হয়, তবেই লোগো দেখাবে
                    if (!product.ImageUrl.StartsWith("http") && !product.ImageUrl.StartsWith("/"))
                    {
                        product.ImageUrl = "/images/default-product.png";
                    }
                }
                else
                {
                    product.ImageUrl = "/images/default-product.png";
                }
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
            else if (string.IsNullOrEmpty(product.ImageUrl))
            {
                product.ImageUrl = "/images/default-product.png";
            }

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
                var productIdString = product.Id?.ToString() ?? "";
                var existingProduct = await _productRepository.GetByIdAsync(productIdString);
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
                else
                {
                    // 🌟 এডিট করার সময়ও যদি ডাটাবেজের আগের নোংরা ইউআরএল থেকে থাকে, তা এখানে ম্যানুয়ালি এডিট না করলেও অটো ব্যাকআপ করবে
                    product.ImageUrl = existingProduct.ImageUrl;
                }

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
}
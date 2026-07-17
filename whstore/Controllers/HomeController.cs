using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using whstore.Models;
using whstore.Services;

namespace whstore.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly Cloudinary _cloudinary;

        public HomeController(IProductRepository productRepository, Cloudinary cloudinary)
        {
            _productRepository = productRepository;
            _cloudinary = cloudinary;
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

        // --- Public Methods (Home Page) ---
        public async Task<IActionResult> Index(string? searchString)
        {
            var allProducts = await _productRepository.GetActiveAsync(searchString ?? "");

            foreach (var product in allProducts)
            {
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    // HTML ইমেজ সোর্স ক্লিনিং লজিক (AliExpress-এর জন্য)
                    if (product.ImageUrl.Contains("<img") || product.ImageUrl.Contains("src="))
                    {
                        var match = Regex.Match(product.ImageUrl, @"src=[""'](?<url>.*?)[""']");
                        if (match.Success) product.ImageUrl = match.Groups["url"].Value;
                    }

                    product.ImageUrl = product.ImageUrl.Replace("\"", "").Replace("'", "").Trim();

                    // ডাবল এক্সটেনশন ফিক্স
                    if (product.ImageUrl.Contains(".png_")) product.ImageUrl = product.ImageUrl.Split(".png_")[0] + ".png";
                    else if (product.ImageUrl.Contains(".jpg_")) product.ImageUrl = product.ImageUrl.Split(".jpg_")[0] + ".jpg";
                    else if (product.ImageUrl.Contains(".jpeg_")) product.ImageUrl = product.ImageUrl.Split(".jpeg_")[0] + ".jpeg";

                    if (product.ImageUrl.StartsWith("//")) product.ImageUrl = "https:" + product.ImageUrl;

                    // 🌟 ফিক্সড: লিংক যদি http/https বা লোকাল পাথ কোনোটা দিয়েই শুরু না হয়, তবেই ফলব্যাক লোগো
                    if (!product.ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !product.ImageUrl.StartsWith("/"))
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

        // --- 🌟 ফিক্সড প্রোডাক্ট আপলোড মেথড 🌟 ---
        [HttpPost]
        public async Task<IActionResult> UploadProduct(Product product, IFormFile? imageFile)
        {
            bool isUploadSuccessful = false;

            // ১. ছবি সিলেক্ট করা থাকলে ক্লাউডিনারিতে আপলোড করার চেষ্টা করবে
            if (imageFile != null && imageFile.Length > 0)
            {
                try
                {
                    using (var stream = imageFile.OpenReadStream())
                    {
                        var uploadParams = new ImageUploadParams()
                        {
                            File = new FileDescription(imageFile.FileName, stream),
                            Folder = "whstore_products"
                        };

                        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                        if (uploadResult != null && uploadResult.SecureUrl != null)
                        {
                            // আপলোড সফল হলে ক্লাউডিনারির সিকিউর লিঙ্ক বসবে
                            product.ImageUrl = uploadResult.SecureUrl.ToString();
                            isUploadSuccessful = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // আপলোডে কোনো ইন্টারনাল এরর হলে ট্র্যাকিং এর জন্য ফলব্যাক রেডি রাখবে
                    isUploadSuccessful = false;
                }
            }

            // 🌟 মূল ফিক্স লজিক: ছবি যদি আপলোড না হয় অথবা কোনো ছবি দেওয়াই না হয়, শুধুমাত্র তখনই লোগো বসবে
            if (!isUploadSuccessful && string.IsNullOrEmpty(product.ImageUrl))
            {
                product.ImageUrl = "/images/default-product.png";
            }

            product.IsActive = true;
            await _productRepository.AddAsync(product);

            TempData["Success"] = "Product uploaded successfully!";
            return RedirectToAction("Privacy");
        }

        // --- Admin Methods ---
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
                    try
                    {
                        using (var stream = imageFile.OpenReadStream())
                        {
                            var uploadParams = new ImageUploadParams()
                            {
                                File = new FileDescription(imageFile.FileName, stream),
                                Folder = "whstore_products"
                            };

                            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                            if (uploadResult != null && uploadResult.SecureUrl != null)
                            {
                                product.ImageUrl = uploadResult.SecureUrl.ToString();
                            }
                        }
                    }
                    catch (Exception)
                    {
                        product.ImageUrl = existingProduct.ImageUrl;
                    }
                }
                else
                {
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
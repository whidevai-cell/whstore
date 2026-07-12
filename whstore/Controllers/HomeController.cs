using Microsoft.AspNetCore.Mvc;
using whstore.Models;
using whstore.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Collections.Generic;
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

        // --- 🔐 গুগলের আন্তর্জাতিক লগইন সিস্টেম অ্যাকশনস ---
        
        // সেটিং আইকন বা লগইন বাটনে ক্লিক করলে এই অ্যাকশনে আসবে
        [HttpGet]
        public IActionResult Login()
        {
            // লগইন সফল হওয়ার পর ইউজারকে সরাসরি "Income" পেজে নিয়ে যাবে
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // গুগলের রেসপন্স রিসিভ করার মেথড
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            if (result.Succeeded)
            {
                // সেশন সাকসেসফুল হলে সরাসরি আপনার ফেসবুক স্টাইলের Income পেজে রিডাইরেক্ট করবে
                TempData["Success"] = "গুগল অ্যাকাউন্ট দিয়ে সফলভাবে লগইন করা হয়েছে!";
                return RedirectToAction("Income");
            }

            return RedirectToAction("Index");
        }

        // লগআউট মেথড
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "সফলভাবে লগআউট করা হয়েছে।";
            return RedirectToAction("Index");
        }

        // ----------------------------------------------------

        // হোমপেজ - প্রোডাক্ট লিস্ট
        public async Task<IActionResult> Index(string? searchString)
        {
            ViewData["CurrentFilter"] = searchString;
            var allProducts = await _productRepository.GetActiveAsync(searchString ?? "");

            var viewModel = new HomeIndexViewModel
            {
                TrendingProducts = allProducts.Where(p => string.IsNullOrEmpty(p.StoreName) || !p.StoreName.Equals("AliExpress", StringComparison.OrdinalIgnoreCase)).ToList(),
                AliExpressProducts = allProducts.Where(p => p.StoreName != null && p.StoreName.Equals("AliExpress", StringComparison.OrdinalIgnoreCase)).ToList(),
                CurrentFilter = searchString
            };

            return View(viewModel);
        }

        // অ্যাডমিন প্যানেল (Privacy)
        public async Task<IActionResult> Privacy()
        {
            try
            {
                var products = await _productRepository.GetProductsForAdminAsync();
                return View(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveProduct(Product product)
        {
            await _productRepository.AddAsync(product);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UploadProduct(Product product, IFormFile? imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

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
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    var url = product.ImageUrl.Trim();
                    if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                        !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                        !url.StartsWith("/"))
                    {
                        product.ImageUrl = null;
                    }
                    else
                    {
                        product.ImageUrl = url;
                    }
                }
                else
                {
                    product.ImageUrl = null;
                }
            }

            product.IsActive = true;
            await _productRepository.AddAsync(product);

            TempData["Success"] = "Product uploaded successfully!";
            return RedirectToAction("Privacy");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _productRepository.DeleteAsync(id);
            if (result)
            {
                TempData["Success"] = "Product deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to delete product.";
            }
            return RedirectToAction("Privacy");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDetected()
        {
            var count = await _productRepository.DeleteDetectedAsync();
            TempData["Success"] = $"Deleted {count} detected products.";
            return RedirectToAction("Privacy");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product)
        {
            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    var url = product.ImageUrl.Trim();
                    if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                        !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                        !url.StartsWith("/"))
                    {
                        product.ImageUrl = null;
                    }
                    else
                    {
                        product.ImageUrl = url;
                    }
                }
                else
                {
                    product.ImageUrl = null;
                }

                var result = await _productRepository.UpdateAsync(product);
                if (result)
                {
                    TempData["Success"] = "Product updated successfully!";
                    return RedirectToAction("Privacy");
                }
                else
                {
                    TempData["Error"] = "Failed to update product.";
                }
            }
            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Product ID cannot be null.");
            }

            var product = await _productRepository.GetByIdAsync(id);

            if (product == null)
            {
                return NotFound();
            }
            
            var relatedProducts = (await _productRepository.GetActiveAsync(""))
                                    .Where(p => p.Id != product.Id)
                                    .Take(12)
                                    .ToList();

            var viewModel = new ProductDetailsViewModel
            {
                Product = product,
                RelatedProducts = relatedProducts
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> LoadMoreProducts(int page = 1, int pageSize = 12)
        {
             var products = (await _productRepository.GetActiveAsync(""))
                              .Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .ToList();
            return PartialView("_ProductCardPartial", products);
        }

        public IActionResult Video()
        {
            return View();
        }

        // --- INCOME (DailyPost) ফিচার ---
        [HttpGet]
        public async Task<IActionResult> Income()
        {
            // 🛠️ ফিক্সড: User.Identity নাল-চেক এবং নাল-ফরগিভিং অপারেটর (!) ব্যবহার করা হয়েছে
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index");
            }

            var posts = new List<DailyPost>(); 
            return View(posts);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePost(string content)
        {
            // 🛠️ ফিক্সড: User.Identity নাল-চেক এবং নাল-ফরগিভিং অপারেটর (!) ব্যবহার করা হয়েছে
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index");
            }

            if (!string.IsNullOrEmpty(content))
            {
                var newPost = new DailyPost
                {
                    Content = content,
                    CreatedAt = DateTime.Now
                };
                
                TempData["Success"] = "স্ট্যাটাসটি সফলভাবে পোস্ট হয়েছে!";
            }

            return RedirectToAction("Income");
        }

        [HttpPost]
        public async Task<IActionResult> Create(EmbedModel model)
        {
            if (ModelState.IsValid)
            {
                TempData["Success"] = "Video added successfully!";
                return RedirectToAction("Video");
            }
            return View("Video", model);
        }
    }
}
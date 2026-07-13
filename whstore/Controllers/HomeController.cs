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
            // সুরক্ষা চেক: যদি product বা ID নাল হয়
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
                else
                {
                    // পুরনো ইমেজ পাথ বজায় রাখা
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

        public async Task<IActionResult> Details(string id) => View(new ProductDetailsViewModel { Product = await _productRepository.GetByIdAsync(id) });

        public IActionResult Video() => View();

        public IActionResult Income() => View(new List<DailyPost>());
    }
}
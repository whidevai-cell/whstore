using Microsoft.AspNetCore.Mvc;
using whstore.Models;
using whstore.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace whstore.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductRepository _productRepository;

        public HomeController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        // হোমপেজ - প্রোডাক্ট লিস্ট
        public async Task<IActionResult> Index(string? searchString)
        {
            ViewData["CurrentFilter"] = searchString;
            var allProducts = await _productRepository.GetActiveAsync(searchString);

            var viewModel = new HomeIndexViewModel
            {
                // For now, all products are trending, but we can add more logic here later
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

                // ছবির URL সেট করার আগে নিশ্চিত করুন যে পুরানো কোনো মান নেই
                product.ImageUrl = "/images/uploads/" + uniqueFileName;
            }
            else
            {
                // যদি কোনো ছবি আপলোড না করা হয় এবং একটি URL দেওয়া হয়, তবে সেটি ভ্যালিডেট করুন
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    var url = product.ImageUrl.Trim();
                    if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                        !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                        !url.StartsWith("/"))
                    {
                        // যদি URL টি ভ্যালিড না হয় তবে null সেট করুন
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

        /*
        [HttpGet]
        public async Task<IActionResult> Diagnose()
        {
            // This method requires direct database access and has been temporarily commented out
            // after refactoring to a repository pattern. It can be reimplemented if needed.
            return Content("Diagnose method is currently disabled.");
        }
        */

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
                // Validate ImageUrl before updating
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    var url = product.ImageUrl.Trim();
                    if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                        !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                        !url.StartsWith("/"))
                    {
                        // If URL is not valid, set to null
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
            
            // Note: The logic to get related products should be moved to the repository
            // For now, we will fetch active products and exclude the current one.
            var relatedProducts = (await _productRepository.GetActiveAsync())
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
             var products = (await _productRepository.GetActiveAsync())
                              .Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .ToList();
            return PartialView("_ProductCardPartial", products);
        }
    }
}

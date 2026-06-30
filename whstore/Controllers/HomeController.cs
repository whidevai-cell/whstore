using Microsoft.AspNetCore.Mvc;
using whstore.Models;
using MongoDB.Driver;

namespace whstore.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMongoCollection<Product> _mongoCollection;

        public HomeController(IMongoDatabase database)
        {
            _mongoCollection = database.GetCollection<Product>("products");
        }

        // হোমপেজ - প্রোডাক্ট লিস্ট
        public async Task<IActionResult> Index(string? searchString)
        {
            var allProducts = await _mongoCollection.Find(_ => true).ToListAsync();

            if (!string.IsNullOrEmpty(searchString))
            {
                allProducts = allProducts.Where(p => (p.Title ?? "").Contains(searchString, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return View(allProducts.OrderByDescending(p => p.LastUpdated).ToList());
        }

        // অ্যাডমিন প্যানেল (Privacy) - এখানে মেথডটি যুক্ত করা হয়েছে
        public async Task<IActionResult> Privacy()
        {
            try
            {
                // শুধুমাত্র সেই প্রোডাক্টগুলো আনবে যেগুলোর টাইটেল "Analyzing Product..." নয় এবং নতুনগুলো প্রথমে দেখাবে
                var products = await _mongoCollection.Find(p => p.Title != "Analyzing Product...")
                    .SortByDescending(p => p.LastUpdated)
                    .ToListAsync();

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
            product.Id = MongoDB.Bson.ObjectId.GenerateNewId();
            product.LastUpdated = DateTime.UtcNow;
            await _mongoCollection.InsertOneAsync(product);
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

            product.Id = MongoDB.Bson.ObjectId.GenerateNewId();
            product.LastUpdated = DateTime.UtcNow;
            product.IsActive = true;
            await _mongoCollection.InsertOneAsync(product);
            
            TempData["Success"] = "Product uploaded successfully!";
            return RedirectToAction("Privacy");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            FilterDefinition<Product> filter;
            if (MongoDB.Bson.ObjectId.TryParse(id, out var objectId))
            {
                filter = Builders<Product>.Filter.Eq(p => p.Id, objectId);
            }
            else if (int.TryParse(id, out var intId))
            {
                filter = Builders<Product>.Filter.Eq(p => p.Id, intId);
            }
            else
            {
                filter = Builders<Product>.Filter.Eq(p => p.Id, id);
            }

            var result = await _mongoCollection.DeleteOneAsync(filter);
            if (result.IsAcknowledged && result.DeletedCount > 0)
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
            var result = await _mongoCollection.DeleteManyAsync(p => 
                (p.Title != null && (p.Title.Contains("Detected") || p.Title.Contains("Analyzing"))) || 
                string.IsNullOrEmpty(p.Title)
            );
            TempData["Success"] = $"Deleted {result.DeletedCount} detected products.";
            return RedirectToAction("Privacy");
        }

        [HttpGet]
        public async Task<IActionResult> Diagnose()
        {
            try
            {
                var dbs = await _mongoCollection.Database.Client.ListDatabaseNamesAsync();
                var dbList = await dbs.ToListAsync();
                var result = new List<object>();
                foreach (var dbName in dbList)
                {
                    var db = _mongoCollection.Database.Client.GetDatabase(dbName);
                    var cols = await db.ListCollectionNamesAsync();
                    var colList = await cols.ToListAsync();
                    var collections = new List<object>();
                    foreach (var colName in colList)
                    {
                        var col = db.GetCollection<MongoDB.Bson.BsonDocument>(colName);
                        var count = await col.CountDocumentsAsync(new MongoDB.Bson.BsonDocument());
                        
                        string sample = "";
                        if (count > 0)
                        {
                            var first = await col.Find(new MongoDB.Bson.BsonDocument()).FirstOrDefaultAsync();
                            sample = first != null ? first.ToString() : "";
                        }
                        
                        collections.Add(new { Collection = colName, Count = count, Sample = sample });
                    }
                    result.Add(new { Database = dbName, Collections = collections });
                }
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        // --- এডিটিং ফিচার ---

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            FilterDefinition<Product> filter;
            if (MongoDB.Bson.ObjectId.TryParse(id, out var objectId))
            {
                filter = Builders<Product>.Filter.Eq(p => p.Id, objectId);
            }
            else if (int.TryParse(id, out var intId))
            {
                filter = Builders<Product>.Filter.Eq(p => p.Id, intId);
            }
            else
            {
                filter = Builders<Product>.Filter.Eq(p => p.Id, id);
            }

            var product = await _mongoCollection.Find(filter).FirstOrDefaultAsync();
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
            product.LastUpdated = DateTime.UtcNow;

            var filter = Builders<Product>.Filter.Eq(p => p.Id, product.Id);
            var result = await _mongoCollection.ReplaceOneAsync(filter, product);

            if (result.IsAcknowledged)
            {
                return RedirectToAction("Privacy");
            }
            return View(product);
        }

        // --- প্রোডাক্ট ডিটেইলস পেজ ---
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Product ID cannot be null.");
            }

            FilterDefinition<Product> filter;
            if (MongoDB.Bson.ObjectId.TryParse(id, out var objectId))
            {
                filter = Builders<Product>.Filter.Eq(p => p.Id, objectId);
            }
            else if (int.TryParse(id, out var intId))
            {
                filter = Builders<Product>.Filter.Eq(p => p.Id, intId);
            }
            else
            {
                filter = Builders<Product>.Filter.Eq(p => p.Id, id);
            }

            var product = await _mongoCollection.Find(filter).FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound();
            }

            // অন্যান্য প্রোডাক্ট লোড করা (প্রাথমিকভাবে ১২টি)
            var relatedProducts = await _mongoCollection.Find(p => p.Id != product.Id && p.IsActive)
                                                      .SortByDescending(p => p.LastUpdated)
                                                      .Limit(12)
                                                      .ToListAsync();

            var viewModel = new ProductDetailsViewModel
            {
                Product = product,
                RelatedProducts = relatedProducts
            };

            return View(viewModel);
        }

        // --- ইনফিনিট স্ক্রোল এর জন্য আরও প্রোডাক্ট লোড করার এন্ডপয়েন্ট ---
        [HttpGet]
        public async Task<IActionResult> LoadMoreProducts(int page = 1, int pageSize = 12)
        {
            var products = await _mongoCollection.Find(p => p.IsActive)
                .SortByDescending(p => p.LastUpdated)
                .Skip((page - 1) * pageSize).Limit(pageSize).ToListAsync();
            return PartialView("_ProductCardPartial", products);
        }
    }
}
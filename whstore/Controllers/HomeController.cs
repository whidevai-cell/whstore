using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using whstore.Models;
using Npgsql;
using System.Data.Common;

namespace whstore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string? _cloudConn;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _cloudConn = _configuration.GetConnectionString("DefaultConnection");
        }

        // ==================== PUBLIC PAGES ====================

        public async Task<IActionResult> Index(string searchString)
        {
            var products = new List<ProductModel>();
            ViewData["CurrentFilter"] = searchString;

            if (string.IsNullOrEmpty(_cloudConn))
            {
                ViewBag.CloudStatus = "OFFLINE";
                return View(products);
            }

            try
            {
                using (var conn = new NpgsqlConnection(_cloudConn))
                {
                    await conn.OpenAsync();
                    string sql = "SELECT * FROM products WHERE isactive = true";

                    if (!string.IsNullOrEmpty(searchString))
                        sql += " AND (LOWER(title) LIKE @search OR LOWER(category) LIKE @search)";

                    sql += " ORDER BY id DESC";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        if (!string.IsNullOrEmpty(searchString))
                            cmd.Parameters.AddWithValue("search", $"%{searchString.Trim().ToLower()}%");

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                products.Add(MapProductFromReader(reader));
                            }
                        }
                    }
                }
                ViewBag.CloudStatus = "ONLINE";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostgreSQL Connection Failed!");
                ViewBag.CloudStatus = "OFFLINE";
            }
            return View(products);
        }

        public async Task<IActionResult> Privacy()
        {
            var products = new List<ProductModel>();

            if (string.IsNullOrEmpty(_cloudConn))
            {
                return View("UserDashboard", products);
            }

            try
            {
                using (var conn = new NpgsqlConnection(_cloudConn))
                {
                    await conn.OpenAsync();
                    string sql = "SELECT * FROM products ORDER BY id DESC";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            products.Add(MapProductFromReader(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Privacy Dashboard Fetch Error");
            }

            return View("UserDashboard", products);
        }

        // ✅ প্রোডাক্ট সেভ — lastupdated সহ
        [HttpPost]
        public async Task<IActionResult> SaveProduct(ProductModel product)
        {
            if (string.IsNullOrEmpty(_cloudConn))
            {
                TempData["Error"] = "❌ Database connection missing!";
                return RedirectToAction("Privacy");
            }

            if (string.IsNullOrWhiteSpace(product.Title))
            {
                TempData["Error"] = "⚠️ Product Title আবশ্যক!";
                return RedirectToAction("Privacy");
            }

            try
            {
                string imageUrl = product.ImageUrl ?? "";
                if (imageUrl.StartsWith("data:image") && imageUrl.Length > 500000)
                {
                    imageUrl = "";
                }

                using (var conn = new NpgsqlConnection(_cloudConn))
                {
                    await conn.OpenAsync();
                    string sql = @"INSERT INTO products 
                                   (title, price, originalprice, imageurl, affiliatelink, category, description, storename, shippingcost, ishotproduct, isactive, lastupdated)  
                                   VALUES (@title, @price, @oprice, @img, @link, @cat, @desc, @store, @ship, @hot, @active, @updated)";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("title", product.Title?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("price", product.Price ?? "0");
                        cmd.Parameters.AddWithValue("oprice", product.OriginalPrice ?? "0");
                        cmd.Parameters.AddWithValue("img", imageUrl);
                        cmd.Parameters.AddWithValue("link", product.AffiliateLink ?? "");
                        cmd.Parameters.AddWithValue("cat", product.Category ?? "General");
                        cmd.Parameters.AddWithValue("desc", product.Description ?? "");
                        cmd.Parameters.AddWithValue("store", product.StoreName ?? "Unknown");
                        cmd.Parameters.AddWithValue("ship", product.ShippingCost ?? "Free");
                        cmd.Parameters.AddWithValue("hot", false);
                        cmd.Parameters.AddWithValue("active", true);
                        cmd.Parameters.AddWithValue("updated", DateTime.UtcNow);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                TempData["Success"] = "✅ Product saved to Cloud successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Save Error — Title: {Title}, Error: {Message}", product.Title, ex.Message);
                TempData["Error"] = $"❌ Save failed! Error: {ex.Message}";
            }

            return RedirectToAction("Privacy");
        }

        public async Task<IActionResult> Details(int id)
        {
            if (string.IsNullOrEmpty(_cloudConn)) return NotFound();

            ProductModel? product = null;
            try
            {
                using (var conn = new NpgsqlConnection(_cloudConn))
                {
                    await conn.OpenAsync();
                    string sql = "SELECT * FROM products WHERE id = @prodId LIMIT 1";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("prodId", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync()) { product = MapProductFromReader(reader); }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Details Fetch Error for ID: {Id}", id);
                return View("Error");
            }

            if (product == null) return NotFound();
            return View(product);
        }

        public IActionResult HealthCheck()
        {
            return Content("Ok");
        }

        [Route("Embed")]
        public IActionResult Embed()
        {
            return View();
        }

        // ==================== SECRET ADMIN PANEL (/whidestore) ====================

        [Route("whidestore")]
        public async Task<IActionResult> SecretDashboard()
        {
            var products = new List<ProductModel>();
            if (string.IsNullOrEmpty(_cloudConn))
            {
                ViewBag.CloudStatus = "OFFLINE";
                return View("Privacy", products);
            }

            try
            {
                using (var conn = new NpgsqlConnection(_cloudConn))
                {
                    await conn.OpenAsync();
                    string sql = "SELECT * FROM products ORDER BY id DESC";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync()) { products.Add(MapProductFromReader(reader)); }
                    }
                }
                ViewBag.CloudStatus = "ONLINE";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard Fetch Error");
                ViewBag.CloudStatus = "OFFLINE";
            }

            ViewBag.TotalProducts = products.Count;
            ViewBag.ActiveProducts = products.Count(p => p.IsActive);
            ViewBag.InactiveProducts = products.Count(p => !p.IsActive);
            return View("Privacy", products);
        }

        [HttpPost]
        [Route("whidestore/delete/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (string.IsNullOrEmpty(_cloudConn)) return Content("DB Error");

            try
            {
                using (var conn = new NpgsqlConnection(_cloudConn))
                {
                    await conn.OpenAsync();
                    string sql = "DELETE FROM products WHERE id = @prodId";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("prodId", id);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                TempData["Success"] = "🗑️ Product deleted!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete Error for ID: {Id}", id);
                TempData["Error"] = "❌ Delete failed!";
            }

            return Redirect("/whidestore");
        }

        [HttpPost]
        [Route("whidestore/toggle/{id}")]
        public async Task<IActionResult> ToggleProduct(int id)
        {
            if (string.IsNullOrEmpty(_cloudConn)) return Content("DB Error");

            try
            {
                using (var conn = new NpgsqlConnection(_cloudConn))
                {
                    await conn.OpenAsync();
                    string sql = "UPDATE products SET isactive = NOT isactive WHERE id = @prodId";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("prodId", id);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                TempData["Success"] = "🔄 Product status toggled!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toggle Error for ID: {Id}", id);
                TempData["Error"] = "❌ Toggle failed!";
            }

            return Redirect("/whidestore");
        }

        [HttpPost]
        [Route("whidestore/update")]
        public async Task<IActionResult> UpdateProduct(ProductModel product)
        {
            if (string.IsNullOrEmpty(_cloudConn)) return Content("DB Error");

            if (product.Id <= 0 || string.IsNullOrWhiteSpace(product.Title))
            {
                TempData["Error"] = "Invalid data!";
                return Redirect("/whidestore");
            }

            try
            {
                using (var conn = new NpgsqlConnection(_cloudConn))
                {
                    await conn.OpenAsync();
                    string sql = @"UPDATE products SET 
                                    title = @title, 
                                    price = @price, 
                                    originalprice = @oprice, 
                                    imageurl = @img, 
                                    affiliatelink = @link, 
                                    category = @cat, 
                                    description = @desc,
                                    storename = @store,
                                    shippingcost = @ship,
                                    lastupdated = @updated
                                   WHERE id = @prodId";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("prodId", product.Id);
                        cmd.Parameters.AddWithValue("title", product.Title?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("price", product.Price ?? "0");
                        cmd.Parameters.AddWithValue("oprice", product.OriginalPrice ?? "0");
                        cmd.Parameters.AddWithValue("img", product.ImageUrl ?? "");
                        cmd.Parameters.AddWithValue("link", product.AffiliateLink ?? "");
                        cmd.Parameters.AddWithValue("cat", product.Category ?? "General");
                        cmd.Parameters.AddWithValue("desc", product.Description ?? "");
                        cmd.Parameters.AddWithValue("store", product.StoreName ?? "Unknown");
                        cmd.Parameters.AddWithValue("ship", product.ShippingCost ?? "Free");
                        cmd.Parameters.AddWithValue("updated", DateTime.UtcNow);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                TempData["Success"] = "✏️ Product updated!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update Error for ID: {Id}", product.Id);
                TempData["Error"] = "❌ Update failed!";
            }

            return Redirect("/whidestore");
        }

        [HttpPost]
        [Route("whidestore/hot/{id}")]
        public async Task<IActionResult> ToggleHotProduct(int id)
        {
            if (string.IsNullOrEmpty(_cloudConn)) return Content("DB Error");

            try
            {
                using (var conn = new NpgsqlConnection(_cloudConn))
                {
                    await conn.OpenAsync();
                    string sql = "UPDATE products SET ishotproduct = NOT ishotproduct WHERE id = @prodId";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("prodId", id);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                TempData["Success"] = "🔥 Hot status toggled!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hot Toggle Error for ID: {Id}", id);
                TempData["Error"] = "❌ Toggle failed!";
            }

            return Redirect("/whidestore");
        }

        // ==================== DATABASE TOOLS ====================

        [Route("fix-db/{key}")]
        public async Task<IActionResult> FixDatabase(string key)
        {
            if (key != "wh786") return NotFound();
            if (string.IsNullOrEmpty(_cloudConn)) return Content("Error: Connection string is missing.");

            try
            {
                using (var conn = new NpgsqlConnection(_cloudConn))
                {
                    await conn.OpenAsync();
                    string sql = @"
                        CREATE TABLE IF NOT EXISTS products (
                            id SERIAL PRIMARY KEY,
                            productid TEXT,
                            title TEXT,
                            price TEXT,
                            originalprice TEXT,
                            imageurl TEXT,
                            affiliatelink TEXT,
                            producturl TEXT,
                            commissionrate TEXT,
                            category TEXT DEFAULT 'General',
                            description TEXT DEFAULT '',
                            storename TEXT DEFAULT 'Global',
                            shippingcost TEXT DEFAULT 'Free',
                            ishotproduct BOOLEAN DEFAULT FALSE,
                            isactive BOOLEAN DEFAULT TRUE,
                            lastupdated TIMESTAMP DEFAULT NOW()
                        );
                        ALTER TABLE products ADD COLUMN IF NOT EXISTS description TEXT DEFAULT '';
                        ALTER TABLE products ADD COLUMN IF NOT EXISTS ishotproduct BOOLEAN DEFAULT FALSE;
                        ALTER TABLE products ADD COLUMN IF NOT EXISTS isactive BOOLEAN DEFAULT TRUE;
                        ALTER TABLE products ADD COLUMN IF NOT EXISTS storename TEXT DEFAULT 'Global';
                        ALTER TABLE products ADD COLUMN IF NOT EXISTS shippingcost TEXT DEFAULT 'Free';
                        ALTER TABLE products ADD COLUMN IF NOT EXISTS lastupdated TIMESTAMP DEFAULT NOW();
                    ";
                    using (var cmd = new NpgsqlCommand(sql, conn)) { await cmd.ExecuteNonQueryAsync(); }
                }
                return Content("Alhamdulillah! Database table & columns updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FixDatabase Error");
                return Content($"Error: {ex.Message}");
            }
        }

        [Route("test-db")]
        public async Task<IActionResult> TestDb()
        {
            if (string.IsNullOrEmpty(_cloudConn)) return Content("❌ Connection string is NULL or empty!");

            try
            {
                using (var conn = new NpgsqlConnection(_cloudConn))
                {
                    await conn.OpenAsync();
                    using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM products", conn))
                    {
                        var count = await cmd.ExecuteScalarAsync();
                        return Content($"✅ DB Connected! Products count: {count}");
                    }
                }
            }
            catch (Exception ex)
            {
                return Content($"❌ DB Error: {ex.Message}");
            }
        }

        // ==================== HELPER METHODS ====================

        private ProductModel MapProductFromReader(DbDataReader reader)
        {
            return new ProductModel
            {
                Id = GetValue(reader, "id") != DBNull.Value ? Convert.ToInt32(GetValue(reader, "id")) : 0,
                ProductId = GetValue(reader, "productid")?.ToString(),
                Title = GetValue(reader, "title")?.ToString() ?? "No Title",
                Price = GetValue(reader, "price")?.ToString() ?? "0",
                OriginalPrice = GetValue(reader, "originalprice")?.ToString() ?? "0",
                ImageUrl = GetValue(reader, "imageurl")?.ToString() ?? "",
                AffiliateLink = GetValue(reader, "affiliatelink")?.ToString() ?? "#",
                ProductUrl = GetValue(reader, "producturl")?.ToString() ?? "#",
                CommissionRate = GetValue(reader, "commissionrate")?.ToString() ?? "0",
                ShippingCost = GetValue(reader, "shippingcost")?.ToString() ?? "Free",
                StoreName = GetValue(reader, "storename")?.ToString() ?? "Global",
                Category = GetValue(reader, "category")?.ToString() ?? "Gadget",
                Description = GetValue(reader, "description")?.ToString() ?? "No description available.",
                IsHotProduct = GetValue(reader, "ishotproduct") != DBNull.Value && Convert.ToBoolean(GetValue(reader, "ishotproduct")),
                IsActive = GetValue(reader, "isactive") != DBNull.Value && Convert.ToBoolean(GetValue(reader, "isactive")),
                LastUpdated = GetValue(reader, "lastupdated") != DBNull.Value ? Convert.ToDateTime(GetValue(reader, "lastupdated")) : DateTime.UtcNow
            };
        }

        private object GetValue(DbDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.GetValue(ordinal);
            }
            catch { return DBNull.Value; }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

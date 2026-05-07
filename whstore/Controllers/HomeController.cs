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

        // ডাটাবেস ফিক্স করার ম্যাজিক রাউট (এটি একবার ব্রাউজারে রান করবেন: /fix-db)
        [Route("fix-db")]
        public async Task<IActionResult> FixDatabase()
        {
            if (string.IsNullOrEmpty(_cloudConn)) return Content("Error: Connection string is missing.");
            try
            {
                using (var conn = new NpgsqlConnection(_cloudConn))
                {
                    await conn.OpenAsync();
                    string sql = @"
                        ALTER TABLE products ADD COLUMN IF NOT EXISTS description TEXT;
                        ALTER TABLE products ADD COLUMN IF NOT EXISTS ishotproduct BOOLEAN DEFAULT FALSE;
                        ALTER TABLE products ADD COLUMN IF NOT EXISTS isactive BOOLEAN DEFAULT TRUE;
                    ";
                    using (var cmd = new NpgsqlCommand(sql, conn)) { await cmd.ExecuteNonQueryAsync(); }
                }
                return Content("Alhamdulillah! Database columns updated successfully.");
            }
            catch (Exception ex) { return Content("Error: " + ex.Message); }
        }

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

        [Route("whidestore")]
        public async Task<IActionResult> SecretDashboard()
        {
            var products = new List<ProductModel>();
            if (string.IsNullOrEmpty(_cloudConn)) return View("Privacy", products);
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
            }
            catch (Exception ex) { _logger.LogError(ex, "Dashboard Fetch Error"); }
            return View("Privacy", products);
        }

        public async Task<IActionResult> Details(int id)
        {
            ProductModel? product = null;
            if (string.IsNullOrEmpty(_cloudConn)) return NotFound();
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
            catch (Exception ex) { _logger.LogError(ex, "Details Fetch Error"); return View("Error"); }
            if (product == null) return NotFound();
            return View(product);
        }

        // --- সেফ ম্যাপিং মেথড (কলাম না থাকলেও ক্র্যাশ করবে না) ---
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

        // কলাম আছে কি না চেক করার হেল্পার
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
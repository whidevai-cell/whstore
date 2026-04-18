using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using whstore.Models;
using Microsoft.Data.Sqlite; // SQLite এর জন্য
using Npgsql; // PostgreSQL এর জন্য

namespace whstore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string? _localConn;
        private readonly string? _cloudConn;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            // appsettings.json থেকে কানেকশন নেওয়া
            _localConn = _configuration.GetConnectionString("DefaultConnection");
            _cloudConn = _configuration.GetConnectionString("SupabaseConnection");
        }

        public async Task<IActionResult> Index()
        {
            var products = new List<ProductModel>();

            // ১. লোকাল SQLite থেকে সব নতুন ফিল্ডসহ ডাটা লোড করা
            try
            {
                if (!string.IsNullOrEmpty(_localConn))
                {
                    using (var conn = new SqliteConnection(_localConn))
                    {
                        await conn.OpenAsync();
                        // সব কলাম সিলেক্ট করা হচ্ছে
                        string sql = "SELECT * FROM products WHERE isactive = 1 ORDER BY id DESC";
                        using (var cmd = new SqliteCommand(sql, conn))
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                products.Add(MapProductFromReader(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Local DB Error: " + ex.Message);
            }

            // ২. লোকাল ডাটা না থাকলে ক্লাউড (Supabase) থেকে আনা
            if (products.Count == 0 && !string.IsNullOrEmpty(_cloudConn))
            {
                try
                {
                    using (var conn = new NpgsqlConnection(_cloudConn))
                    {
                        await conn.OpenAsync();
                        string sql = "SELECT * FROM products WHERE isactive = true ORDER BY id DESC";
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
                    _logger.LogError(ex, "Cloud Storage Offline!");
                    ViewBag.ErrorMessage = "Cloud Sync Offline.";
                }
            }

            return View(products);
        }

        // ডাটাবেস থেকে মডেল ম্যাপ করার জন্য একটি কমন মেথড
        private ProductModel MapProductFromReader(System.Data.Common.DbDataReader reader)
        {
            return new ProductModel
            {
                Id = Convert.ToInt32(reader["id"]),
                ProductId = reader["id"]?.ToString(),
                Title = reader["title"]?.ToString() ?? "No Title",
                Price = reader["price"]?.ToString() ?? "0",
                OriginalPrice = reader["originalprice"]?.ToString(),
                ImageUrl = reader["imageurl"]?.ToString() ?? "",
                AffiliateLink = reader["affiliatelink"]?.ToString() ?? "#",
                CommissionRate = reader["commissionrate"]?.ToString(),
                ShippingCost = reader["shippingcost"]?.ToString(),
                StoreName = reader["storename"]?.ToString(),
                IsHotProduct = reader["hotproduct"] != DBNull.Value && Convert.ToBoolean(reader["hotproduct"]),
                LastUpdated = reader["lastupdated"] != DBNull.Value ? Convert.ToDateTime(reader["lastupdated"]) : DateTime.UtcNow
            };
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
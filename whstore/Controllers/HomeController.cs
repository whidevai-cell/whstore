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
            // appsettings.json থেকে PostgreSQL কানেকশন স্ট্রিং নেওয়া হচ্ছে
            _cloudConn = _configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IActionResult> Index()
        {
            var products = new List<ProductModel>();

            if (string.IsNullOrEmpty(_cloudConn))
            {
                ViewBag.CloudStatus = "OFFLINE";
                _logger.LogError("Cloud Connection String (DefaultConnection) is missing!");
                return View(products);
            }

            try
            {
                using (var conn = new NpgsqlConnection(_cloudConn))
                {
                    await conn.OpenAsync();

                    // PostgreSQL কোয়েরি: শুধুমাত্র একটিভ প্রোডাক্টগুলো দেখাবে
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
                ViewBag.CloudStatus = "ONLINE";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostgreSQL Connection Failed!");
                ViewBag.CloudStatus = "OFFLINE";
                ViewBag.ErrorMessage = "Database connection error.";
            }

            return View(products);
        }

        // ডাটাবেস থেকে মডেল ম্যাপ করার মেথড (Error Safe)
        private ProductModel MapProductFromReader(DbDataReader reader)
        {
            return new ProductModel
            {
                Id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0,
                ProductId = reader["productid"]?.ToString(),
                Title = reader["title"]?.ToString() ?? "No Title",
                Price = reader["price"]?.ToString() ?? "0",
                OriginalPrice = reader["originalprice"]?.ToString() ?? "0",
                ImageUrl = reader["imageurl"]?.ToString() ?? "",
                AffiliateLink = reader["affiliatelink"]?.ToString() ?? "#",
                ProductUrl = reader["producturl"]?.ToString() ?? "#",
                CommissionRate = reader["commissionrate"]?.ToString() ?? "0",
                ShippingCost = reader["shippingcost"]?.ToString() ?? "Free",
                StoreName = reader["storename"]?.ToString() ?? "Global",
                Category = reader["category"]?.ToString() ?? "Gadget",
                // PostgreSQL এর বুলিয়ান কলাম চেক
                IsHotProduct = reader["ishotproduct"] != DBNull.Value && Convert.ToBoolean(reader["ishotproduct"]),
                IsActive = reader["isactive"] != DBNull.Value && Convert.ToBoolean(reader["isactive"]),
                LastUpdated = reader["lastupdated"] != DBNull.Value ? Convert.ToDateTime(reader["lastupdated"]) : DateTime.UtcNow
            };
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
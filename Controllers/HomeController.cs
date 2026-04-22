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
            // appsettings.json থেকে কানেকশন স্ট্রিং নেওয়া হচ্ছে
            _cloudConn = _configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IActionResult> Index()
        {
            var products = new List<ProductModel>();

            if (string.IsNullOrEmpty(_cloudConn))
            {
                ViewBag.CloudStatus = "OFFLINE";
                _logger.LogError("Cloud Connection String is missing in appsettings.json!");
                return View(products);
            }

            try
            {
                using (var conn = new NpgsqlConnection(_cloudConn))
                {
                    await conn.OpenAsync();
                    
                    // এখানে query-তে আপনার টেবিলের নাম এবং কলাম নিশ্চিত করুন
                    // 'isactive' কলাম না থাকলে শুধু 'SELECT * FROM products ORDER BY id DESC' দিন
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
                ViewBag.CloudStatus = "ONLINE";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cloud Storage Connection Failed!");
                ViewBag.CloudStatus = "OFFLINE";
                ViewBag.ErrorMessage = "Database connection failed. " + ex.Message;
            }

            return View(products);
        }

        // ডাটাবেস থেকে মডেল ম্যাপ করার মেথড (Error Safe)
        private ProductModel MapProductFromReader(DbDataReader reader)
        {
            return new ProductModel
            {
                // ডাটাবেসের কলামের নাম যদি ছোট হাতের হয় তবে এখানেও ছোট হাতের দিতে হবে
                Id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0,
                ProductId = reader["id"]?.ToString(),
                Title = reader["title"]?.ToString() ?? "No Title",
                Price = reader["price"]?.ToString() ?? "0",
                OriginalPrice = reader["originalprice"]?.ToString() ?? "0",
                ImageUrl = reader["imageurl"]?.ToString() ?? "",
                AffiliateLink = reader["affiliatelink"]?.ToString() ?? "#",
                CommissionRate = reader["commissionrate"]?.ToString() ?? "0%",
                ShippingCost = reader["shippingcost"]?.ToString() ?? "Free",
                StoreName = reader["storename"]?.ToString() ?? "Global",
                Category = reader["category"]?.ToString() ?? "Gadget",
                IsHotProduct = reader["ishotproduct"] != DBNull.Value && Convert.ToBoolean(reader["ishotproduct"]),
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

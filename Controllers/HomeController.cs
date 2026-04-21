using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using whstore.Models;
using Npgsql; // শুধুমাত্র PostgreSQL ব্যবহার করবো

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
            // রেন্ডার বা ক্লাউড ডাটাবেসের জন্য DefaultConnection ব্যবহার করা হচ্ছে
            _cloudConn = _configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IActionResult> Index()
        {
            var products = new List<ProductModel>();

            if (string.IsNullOrEmpty(_cloudConn))
            {
                ViewBag.CloudStatus = "OFFLINE";
                _logger.LogError("Cloud Connection String is missing!");
                return View(products);
            }

            try
            {
                using (var conn = new NpgsqlConnection(_cloudConn))
                {
                    await conn.OpenAsync();
                    // PostgreSQL এর জন্য কোয়েরি (isactive = true)
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
                _logger.LogError(ex, "Cloud Storage Connection Failed!");
                ViewBag.CloudStatus = "OFFLINE";
                ViewBag.ErrorMessage = "Database connection failed.";
            }

            return View(products);
        }

        // ডাটাবেস থেকে মডেল ম্যাপ করার মেথড
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
                // PostgreSQL এ বুলিয়ান ভ্যালু চেক
                IsHotProduct = reader["hotproduct"] != DBNull.Value && Convert.ToBoolean(reader["hotproduct"]),
                LastUpdated = reader["lastupdated"] != DBNull.Value ? Convert.ToDateTime(reader["lastupdated"]) : DateTime.UtcNow
            };
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

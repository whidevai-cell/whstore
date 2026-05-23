using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data.Common;
using whstore.Models;
using whstore.Filters;

namespace whstore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductSyncController : ControllerBase
    {
        private readonly ILogger<ProductSyncController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string? _cloudConn;

        public ProductSyncController(ILogger<ProductSyncController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _cloudConn = _configuration.GetConnectionString("DefaultConnection");
        }

        // ==================== GET ALL PRODUCTS ====================
        // GET: /api/ProductSync/get-all
        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll()
        {
            var products = new List<object>();

            if (string.IsNullOrEmpty(_cloudConn))
                return StatusCode(500, new { error = "Database connection not configured." });

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
                            products.Add(MapToObject(reader));
                        }
                    }
                }
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAll Error");
                return StatusCode(500, new { error = "Failed to fetch products." });
            }
        }

        // ==================== GET ACTIVE PRODUCTS ONLY ====================
        // GET: /api/ProductSync/get-active
        [HttpGet("get-active")]
        public async Task<IActionResult> GetActive()
        {
            var products = new List<object>();

            if (string.IsNullOrEmpty(_cloudConn))
                return StatusCode(500, new { error = "Database connection not configured." });

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
                            products.Add(MapToObject(reader));
                        }
                    }
                }
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetActive Error");
                return StatusCode(500, new { error = "Failed to fetch active products." });
            }
        }

        // ==================== CREATE / SYNC PRODUCT ====================
        // POST: /api/ProductSync/sync-built-in
        [HttpPost("sync-built-in")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> SyncBuiltIn([FromBody] ProductDTO dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.title))
                return BadRequest(new { error = "Title is required." });

            if (string.IsNullOrEmpty(_cloudConn))
                return StatusCode(500, new { error = "Database connection not configured." });

            try
            {
                using (var conn = new NpgsqlConnection(_cloudConn))
                {
                    await conn.OpenAsync();
                    string sql = @"INSERT INTO products 
                        (title, description, price, originalprice, imageurl, affiliatelink, 
                         commissionrate, shippingcost, storename, category, 
                         reviewcount, reviewrate, attributes, ishotproduct, isactive, lastupdated)
                        VALUES 
                        (@title, @desc, @price, @oprice, @img, @link, 
                         @commission, @shipping, @store, @cat,
                         @reviewcount, @reviewrate, @attributes, @hot, true, @updated)";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("title", dto.title?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("desc", dto.description ?? "");
                        cmd.Parameters.AddWithValue("price", dto.price ?? "0");
                        cmd.Parameters.AddWithValue("oprice", dto.originalprice ?? "0");
                        cmd.Parameters.AddWithValue("img", dto.imageurl ?? "");
                        cmd.Parameters.AddWithValue("link", dto.affiliatelink ?? "");
                        cmd.Parameters.AddWithValue("commission", dto.commissionrate ?? "0");
                        cmd.Parameters.AddWithValue("shipping", dto.shippingcost ?? "Free");
                        cmd.Parameters.AddWithValue("store", dto.storename ?? "Global");
                        cmd.Parameters.AddWithValue("cat", dto.category ?? "General");
                        cmd.Parameters.AddWithValue("reviewcount", dto.reviewcount ?? "0");
                        cmd.Parameters.AddWithValue("reviewrate", dto.reviewrate ?? "0");
                        cmd.Parameters.AddWithValue("attributes", dto.attributes ?? "");
                        cmd.Parameters.AddWithValue("hot", dto.ishotproduct);
                        cmd.Parameters.AddWithValue("updated", DateTime.UtcNow);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return Ok(new { message = "Product saved successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SyncBuiltIn Error");
                return StatusCode(500, new { error = "Failed to save product." });
            }
        }

        // ==================== DELETE PRODUCT ====================
        // DELETE: /api/ProductSync/delete/{id}
        [HttpDelete("delete/{id}")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (string.IsNullOrEmpty(_cloudConn))
                return StatusCode(500, new { error = "Database connection not configured." });

            try
            {
                using (var conn = new NpgsqlConnection(_cloudConn))
                {
                    await conn.OpenAsync();
                    string sql = "DELETE FROM products WHERE id = @prodId";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("prodId", id);
                        int affected = await cmd.ExecuteNonQueryAsync();

                        if (affected == 0)
                            return NotFound(new { error = $"Product with ID {id} not found." });
                    }
                }
                return Ok(new { message = $"Product {id} deleted successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete Error for ID: {Id}", id);
                return StatusCode(500, new { error = "Failed to delete product." });
            }
        }

        // ==================== TOGGLE ACTIVE/INACTIVE ====================
        // PUT: /api/ProductSync/toggle/{id}
        [HttpPut("toggle/{id}")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> ToggleProduct(int id)
        {
            if (string.IsNullOrEmpty(_cloudConn))
                return StatusCode(500, new { error = "Database connection not configured." });

            try
            {
                using (var conn = new NpgsqlConnection(_cloudConn))
                {
                    await conn.OpenAsync();
                    string sql = "UPDATE products SET isactive = NOT isactive, lastupdated = @updated WHERE id = @prodId";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("prodId", id);
                        cmd.Parameters.AddWithValue("updated", DateTime.UtcNow);
                        int affected = await cmd.ExecuteNonQueryAsync();

                        if (affected == 0)
                            return NotFound(new { error = $"Product with ID {id} not found." });
                    }
                }
                return Ok(new { message = $"Product {id} toggled successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toggle Error for ID: {Id}", id);
                return StatusCode(500, new { error = "Failed to toggle product." });
            }
        }

        // ==================== UPDATE PRODUCT ====================
        // PUT: /api/ProductSync/update/{id}
        [HttpPut("update/{id}")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductDTO dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.title))
                return BadRequest(new { error = "Title is required." });

            if (string.IsNullOrEmpty(_cloudConn))
                return StatusCode(500, new { error = "Database connection not configured." });

            try
            {
                using (var conn = new NpgsqlConnection(_cloudConn))
                {
                    await conn.OpenAsync();
                    string sql = @"UPDATE products SET 
                        title = @title,
                        description = @desc,
                        price = @price,
                        originalprice = @oprice,
                        imageurl = @img,
                        affiliatelink = @link,
                        category = @cat,
                        storename = @store,
                        ishotproduct = @hot,
                        lastupdated = @updated
                        WHERE id = @prodId";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("prodId", id);
                        cmd.Parameters.AddWithValue("title", dto.title?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("desc", dto.description ?? "");
                        cmd.Parameters.AddWithValue("price", dto.price ?? "0");
                        cmd.Parameters.AddWithValue("oprice", dto.originalprice ?? "0");
                        cmd.Parameters.AddWithValue("img", dto.imageurl ?? "");
                        cmd.Parameters.AddWithValue("link", dto.affiliatelink ?? "");
                        cmd.Parameters.AddWithValue("cat", dto.category ?? "General");
                        cmd.Parameters.AddWithValue("store", dto.storename ?? "Global");
                        cmd.Parameters.AddWithValue("hot", dto.ishotproduct);
                        cmd.Parameters.AddWithValue("updated", DateTime.UtcNow);

                        int affected = await cmd.ExecuteNonQueryAsync();

                        if (affected == 0)
                            return NotFound(new { error = $"Product with ID {id} not found." });
                    }
                }
                return Ok(new { message = $"Product {id} updated successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update Error for ID: {Id}", id);
                return StatusCode(500, new { error = "Failed to update product." });
            }
        }

        // ==================== GET SINGLE PRODUCT ====================
        // GET: /api/ProductSync/get/{id}
        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (string.IsNullOrEmpty(_cloudConn))
                return StatusCode(500, new { error = "Database connection not configured." });

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
                            if (await reader.ReadAsync())
                            {
                                return Ok(MapToObject(reader));
                            }
                        }
                    }
                }
                return NotFound(new { error = $"Product with ID {id} not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetById Error for ID: {Id}", id);
                return StatusCode(500, new { error = "Failed to fetch product." });
            }
        }

        // ==================== HELPER: Map Reader to Object ====================
        private object MapToObject(DbDataReader reader)
        {
            return new
            {
                id = GetVal(reader, "id") != DBNull.Value ? Convert.ToInt32(GetVal(reader, "id")) : 0,
                productid = GetVal(reader, "productid")?.ToString(),
                title = GetVal(reader, "title")?.ToString() ?? "No Title",
                description = GetVal(reader, "description")?.ToString() ?? "",
                price = GetVal(reader, "price")?.ToString() ?? "0",
                originalprice = GetVal(reader, "originalprice")?.ToString() ?? "0",
                imageurl = GetVal(reader, "imageurl")?.ToString() ?? "",
                affiliatelink = GetVal(reader, "affiliatelink")?.ToString() ?? "#",
                producturl = GetVal(reader, "producturl")?.ToString() ?? "#",
                commissionrate = GetVal(reader, "commissionrate")?.ToString() ?? "0",
                shippingcost = GetVal(reader, "shippingcost")?.ToString() ?? "Free",
                storename = GetVal(reader, "storename")?.ToString() ?? "Global",
                category = GetVal(reader, "category")?.ToString() ?? "General",
                ishotproduct = GetVal(reader, "ishotproduct") != DBNull.Value && Convert.ToBoolean(GetVal(reader, "ishotproduct")),
                isactive = GetVal(reader, "isactive") != DBNull.Value && Convert.ToBoolean(GetVal(reader, "isactive")),
                lastupdated = GetVal(reader, "lastupdated") != DBNull.Value ? Convert.ToDateTime(GetVal(reader, "lastupdated")).ToString("yyyy-MM-dd HH:mm") : ""
            };
        }

        private object GetVal(DbDataReader reader, string col)
        {
            try
            {
                int ordinal = reader.GetOrdinal(col);
                return reader.GetValue(ordinal);
            }
            catch { return DBNull.Value; }
        }
    }
}

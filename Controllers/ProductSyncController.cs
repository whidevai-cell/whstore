using Microsoft.AspNetCore.Mvc;
using whstore.Models;
using Npgsql; // PostgreSQL (Supabase) এর জন্য
using Microsoft.Data.Sqlite; // SQLite (Local) এর জন্য
using System;
using System.Threading.Tasks;

namespace whstore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductSyncController : ControllerBase
    {
        private readonly string _localConn;
        private readonly string _cloudConn;

        public ProductSyncController(IConfiguration configuration)
        {
            _localConn = configuration.GetConnectionString("DefaultConnection"); // SQLite
            _cloudConn = configuration.GetConnectionString("SupabaseConnection"); // Supabase
        }

        [HttpPost("sync-built-in")]
        public async Task<IActionResult> SyncFromInternalDashboard([FromBody] ProductModel product)
        {
            if (product == null)
                return BadRequest(new { success = false, message = "ড্যাশবোর্ড থেকে কোনো ডাটা পাওয়া যায়নি!" });

            bool localSuccess = false;
            bool cloudSuccess = false;
            string errorMessage = "";

            // ১. লোকাল SQLite ডাটাবেসে সেভ করা
            try
            {
                using var lConn = new SqliteConnection(_localConn);
                await lConn.OpenAsync();

                string localSql = @"
                    CREATE TABLE IF NOT EXISTS products (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        title TEXT UNIQUE,
                        price TEXT,
                        originalprice TEXT,
                        imageurl TEXT,
                        affiliatelink TEXT,
                        commissionrate TEXT,
                        evaluationcount TEXT,
                        goodreviewrate TEXT,
                        shippingcost TEXT,
                        storename TEXT,
                        categoryid TEXT,
                        attributes TEXT,
                        hotproduct INTEGER DEFAULT 0,
                        isactive INTEGER DEFAULT 1,
                        lastupdated TEXT
                    );
                    INSERT INTO products (
                        title, price, originalprice, imageurl, affiliatelink, 
                        commissionrate, evaluationcount, goodreviewrate, shippingcost, 
                        storename, categoryid, attributes, hotproduct, lastupdated
                    ) 
                    VALUES (
                        @title, @price, @oPrice, @img, @link, 
                        @comm, @revCount, @revRate, @ship, 
                        @store, @cat, @attr, @hot, @updated
                    )
                    ON CONFLICT(title) DO UPDATE SET
                        price=excluded.price,
                        imageurl=excluded.imageurl,
                        affiliatelink=excluded.affiliatelink,
                        lastupdated=excluded.lastupdated;";

                using var lCmd = new SqliteCommand(localSql, lConn);
                AddParameters(lCmd, product, true); // SQLite এর জন্য true পাঠাচ্ছি

                await lCmd.ExecuteNonQueryAsync();
                localSuccess = true;
            }
            catch (Exception ex)
            {
                errorMessage += "Local Save Error: " + ex.Message + " ";
            }

            // ২. ক্লাউড (Supabase) ব্যাকআপ
            try
            {
                if (!string.IsNullOrEmpty(_cloudConn))
                {
                    using var cConn = new NpgsqlConnection(_cloudConn);
                    await cConn.OpenAsync();

                    string cloudSql = @"
                        INSERT INTO products (
                            title, price, originalprice, imageurl, affiliatelink, 
                            commissionrate, evaluationcount, goodreviewrate, shippingcost, 
                            storename, categoryid, attributes, hotproduct, lastupdated, source
                        ) 
                        VALUES (
                            @title, @price, @oPrice, @img, @link, 
                            @comm, @revCount, @revRate, @ship, 
                            @store, @cat, @attr, @hot, @updated, @src
                        )
                        ON CONFLICT(title) DO UPDATE SET 
                            price=EXCLUDED.price, imageurl=EXCLUDED.imageurl, 
                            lastupdated=EXCLUDED.lastupdated;";

                    using var cCmd = new NpgsqlCommand(cloudSql, cConn);
                    AddParameters(cCmd, product, false); // Cloud এর জন্য false
                    cCmd.Parameters.AddWithValue("@src", "Smart-Edit-v2.5");

                    await cCmd.ExecuteNonQueryAsync();
                    cloudSuccess = true;
                }
            }
            catch (Exception)
            {
                errorMessage += "Cloud Backup Failed (Offline). ";
            }

            if (localSuccess)
            {
                return Ok(new
                {
                    success = true,
                    message = cloudSuccess ? "Saved to Both Local & Cloud!" : "Saved Locally (Cloud Offline)",
                    timestamp = DateTime.UtcNow.AddHours(6).ToString("hh:mm tt")
                });
            }

            return StatusCode(500, new { success = false, error = errorMessage });
        }

        private void AddParameters(System.Data.Common.DbCommand cmd, ProductModel p, bool isSqlite)
        {
            // @pId আমরা আর ব্যবহার করছি না, তাই এটা ফেলে দেওয়া হয়েছে
            cmd.Parameters.Add(CreateParam(cmd, "@title", p.Title ?? ""));
            cmd.Parameters.Add(CreateParam(cmd, "@price", p.Price ?? ""));
            cmd.Parameters.Add(CreateParam(cmd, "@oPrice", p.OriginalPrice ?? ""));
            cmd.Parameters.Add(CreateParam(cmd, "@img", p.ImageUrl ?? ""));
            cmd.Parameters.Add(CreateParam(cmd, "@link", p.AffiliateLink ?? ""));
            cmd.Parameters.Add(CreateParam(cmd, "@comm", p.CommissionRate ?? ""));
            cmd.Parameters.Add(CreateParam(cmd, "@revCount", p.ReviewCount ?? ""));
            cmd.Parameters.Add(CreateParam(cmd, "@revRate", p.ReviewRate ?? ""));
            cmd.Parameters.Add(CreateParam(cmd, "@ship", p.ShippingCost ?? ""));
            cmd.Parameters.Add(CreateParam(cmd, "@store", p.StoreName ?? ""));
            cmd.Parameters.Add(CreateParam(cmd, "@cat", p.Category ?? ""));
            cmd.Parameters.Add(CreateParam(cmd, "@attr", p.Attributes ?? ""));
            cmd.Parameters.Add(CreateParam(cmd, "@hot", p.IsHotProduct ? 1 : 0));
            cmd.Parameters.Add(CreateParam(cmd, "@updated", DateTime.UtcNow.AddHours(6).ToString("yyyy-MM-dd HH:mm:ss")));
        }

        private System.Data.Common.DbParameter CreateParam(System.Data.Common.DbCommand cmd, string name, object value)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = name;
            param.Value = value;
            return param;
        }
    }
}
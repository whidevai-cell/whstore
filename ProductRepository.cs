using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using whstore.Data;
using whstore.Models;

namespace whstore.Services
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ProductRepository> _logger;

        public ProductRepository(ApplicationDbContext db, ILogger<ProductRepository> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger;
        }

        public async Task<List<ProductModel>> GetActiveAsync(string? search = null)
        {
            try
            {
                var q = _db.Products.AsQueryable().Where(p => p.IsActive);
                if (!string.IsNullOrEmpty(search))
                {
                    var s = search.Trim().ToLower();
                    q = q.Where(p => (p.Title ?? "").ToLower().Contains(s) ||
                                     (p.Category ?? "").ToLower().Contains(s));
                }
                return await q.OrderByDescending(p => p.Id).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetActiveAsync failed");
                return new List<ProductModel>();
            }
        }

        public async Task<List<ProductModel>> GetAllAsync()
        {
            try
            {
                return await _db.Products.OrderByDescending(p => p.Id).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllAsync failed");
                return new List<ProductModel>();
            }
        }

        public async Task<ProductModel?> GetByIdAsync(int id)
        {
            try
            {
                return await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetByIdAsync failed");
                return null;
            }
        }

        public async Task AddAsync(ProductModel product)
        {
            if (product == null) return;
            try
            {
                _db.Products.Add(product);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddAsync failed");
            }
        }

        public async Task EnsureSchemaAsync()
        {
            try
            {
                // Safe ALTER statements for Postgres — run only if connected.
                var sql = @"
                    ALTER TABLE products ADD COLUMN IF NOT EXISTS description TEXT;
                    ALTER TABLE products ADD COLUMN IF NOT EXISTS ishotproduct BOOLEAN DEFAULT FALSE;
                    ALTER TABLE products ADD COLUMN IF NOT EXISTS isactive BOOLEAN DEFAULT TRUE;
                ";
                await _db.Database.ExecuteSqlRawAsync(sql);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EnsureSchemaAsync failed");
            }
        }
    }
}
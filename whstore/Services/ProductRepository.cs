using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using whstore.Models;

namespace whstore.Services
{
    public class ProductRepository : IProductRepository
    {
        private readonly IMongoCollection<Product> _products;

        public ProductRepository(IMongoDatabase database)
        {
            _products = database.GetCollection<Product>("products");
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _products.Find(_ => true).ToListAsync();
        }

        public async Task<List<Product>> GetActiveAsync(string? search = null)
        {
            var filter = Builders<Product>.Filter.Eq(p => p.IsActive, true);
            if (!string.IsNullOrEmpty(search))
            {
                var searchFilter = Builders<Product>.Filter.Regex(p => p.Title, new MongoDB.Bson.BsonRegularExpression(search, "i"));
                filter &= searchFilter;
            }
            return await _products.Find(filter).SortByDescending(p => p.LastUpdated).ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(string id)
        {
            if (MongoDB.Bson.ObjectId.TryParse(id, out var objectId))
            {
                var filter = Builders<Product>.Filter.Eq(p => p.Id, objectId);
                return await _products.Find(filter).FirstOrDefaultAsync();
            }
            return null;
        }

        public async Task AddAsync(Product product)
        {
            product.Id = MongoDB.Bson.ObjectId.GenerateNewId();
            product.LastUpdated = DateTime.UtcNow;
            await _products.InsertOneAsync(product);
        }

        public async Task<bool> UpdateAsync(Product product)
        {
            product.LastUpdated = DateTime.UtcNow;
            var filter = Builders<Product>.Filter.Eq(p => p.Id, product.Id);
            var result = await _products.ReplaceOneAsync(filter, product);
            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            if (MongoDB.Bson.ObjectId.TryParse(id, out var objectId))
            {
                var filter = Builders<Product>.Filter.Eq(p => p.Id, objectId);
                var result = await _products.DeleteOneAsync(filter);
                return result.IsAcknowledged && result.DeletedCount > 0;
            }
            return false;
        }

        public Task EnsureSchemaAsync()
        {
            // For MongoDB, schema is flexible. This method can be used for creating indexes if needed.
            // For now, it's not strictly necessary.
            return Task.CompletedTask;
        }

        public async Task<List<Product>> GetProductsForAdminAsync()
        {
            return await _products.Find(p => p.Title != "Analyzing Product...")
                .SortByDescending(p => p.LastUpdated)
                .ToListAsync();
        }

        public async Task<long> DeleteDetectedAsync()
        {
            var result = await _products.DeleteManyAsync(p =>
                (p.Title != null && (p.Title.Contains("Detected") || p.Title.Contains("Analyzing"))) ||
                string.IsNullOrEmpty(p.Title)
            );
            return result.DeletedCount;
        }
    }
}

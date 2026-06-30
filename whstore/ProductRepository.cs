using MongoDB.Driver;
using whstore.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using MongoDB.Bson;

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
            // ফিল্টার বাদ দিয়ে সব ডাটা নিয়ে আসার জন্য পরিবর্তন করা হয়েছে
            var filter = Builders<Product>.Filter.Empty;

            if (!string.IsNullOrWhiteSpace(search))
            {
                filter = Builders<Product>.Filter.Regex(p => p.Title, new MongoDB.Bson.BsonRegularExpression(search, "i"));
            }

            return await _products.Find(filter).ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(string id)
        {
            FilterDefinition<Product> filter;
            if (ObjectId.TryParse(id, out var objectId))
            {
                filter = Builders<Product>.Filter.Eq(p => p.Id, objectId);
            }
            else
            {
                filter = Builders<Product>.Filter.Eq(p => p.Id, id);
            }
            return await _products.Find(filter).FirstOrDefaultAsync();
        }

        public async Task AddAsync(Product product)
        {
            await _products.InsertOneAsync(product);
        }

        public async Task<bool> UpdateAsync(Product product)
        {
            var filter = Builders<Product>.Filter.Eq(p => p.Id, product.Id);
            var result = await _products.ReplaceOneAsync(filter, product);
            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            FilterDefinition<Product> filter;
            if (ObjectId.TryParse(id, out var objectId))
            {
                filter = Builders<Product>.Filter.Eq(p => p.Id, objectId);
            }
            else
            {
                filter = Builders<Product>.Filter.Eq(p => p.Id, id);
            }
            var result = await _products.DeleteOneAsync(filter);
            return result.IsAcknowledged && result.DeletedCount > 0;
        }

        public Task EnsureSchemaAsync() => Task.CompletedTask;
    }
}
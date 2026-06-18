using MongoDB.Driver;
using whstore.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace whstore.Services
{
    public class ProductRepository : IProductRepository
    {
        private readonly IMongoCollection<Product> _products;

        public ProductRepository(IMongoDatabase database)
        {
            // MongoDB-এর কালেকশন "Products" এ আপডেট করা হলো
            _products = database.GetCollection<Product>("Products");
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
            return await _products.Find(p => p.Id == id).FirstOrDefaultAsync();
        }

        public async Task AddAsync(Product product)
        {
            await _products.InsertOneAsync(product);
        }

        public async Task<bool> UpdateAsync(Product product)
        {
            var result = await _products.ReplaceOneAsync(p => p.Id == product.Id, product);
            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _products.DeleteOneAsync(p => p.Id == id);
            return result.IsAcknowledged && result.DeletedCount > 0;
        }

        public Task EnsureSchemaAsync() => Task.CompletedTask;
    }
}
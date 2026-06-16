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
            // MongoDB-এর কালেকশন সিলেক্ট করা হচ্ছে
            _products = database.GetCollection<Product>("AffiliateProducts");
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _products.Find(_ => true).ToListAsync();
        }

        public async Task<List<Product>> GetActiveAsync(string? search = null)
        {
            var filter = Builders<Product>.Filter.Eq(p => p.IsActive, true);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchFilter = Builders<Product>.Filter.Regex(p => p.Title, new MongoDB.Bson.BsonRegularExpression(search, "i"));
                filter &= searchFilter;
            }

            return await _products.Find(filter).ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(string id)
        {
            return await _products.Find(p => p.Id == id).FirstOrDefaultAsync();
        }

        public async Task AddAsync(Product product)
        {
            // MongoDB নিজেই আইডি জেনারেট করে নেবে
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

        // আপনার যদি পুরনো ইন্টারফেসের জন্য GetByIdAsync(int id) দরকার হয় (যা এখন আর প্রয়োজন নেই)
        // তবে সেটি এখান থেকে মুছে ফেলুন অথবা ডিলিট করে দিন।
    }
}
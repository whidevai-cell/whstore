using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using whstore.Models;
using whstore.Services;
using MongoDB.Bson;

namespace whstore.Services
{
    public class ProductRepository : IProductRepository
    {
        private readonly IMongoCollection<Product> _products;
        private readonly IMongoCollection<EmbedModel> _videos;

        public ProductRepository(IMongoDatabase database)
        {
            _products = database.GetCollection<Product>("products");
            _videos = database.GetCollection<EmbedModel>("videos");
        }

        public async Task<IEnumerable<Product>> GetAllAsync() => await _products.Find(_ => true).ToListAsync();

        public async Task<IEnumerable<Product>> GetActiveAsync(string searchString)
        {
            var filter = Builders<Product>.Filter.Eq(p => p.IsActive, true);
            if (!string.IsNullOrEmpty(searchString))
            {
                filter &= Builders<Product>.Filter.Regex(p => p.Title, new BsonRegularExpression(searchString, "i"));
            }
            return await _products.Find(filter).SortByDescending(p => p.LastUpdated).ToListAsync();
        }

        public async Task<Product> GetByIdAsync(string id)
        {
            if (ObjectId.TryParse(id, out var objectId))
            {
                var filter = Builders<Product>.Filter.Eq(p => p.Id, objectId);
                var result = await _products.Find(filter).FirstOrDefaultAsync();
                return result ?? new Product();
            }
            return new Product();
        }

        public async Task AddAsync(Product product)
        {
            product.Id = ObjectId.GenerateNewId();
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
            if (ObjectId.TryParse(id, out var objectId))
            {
                var filter = Builders<Product>.Filter.Eq(p => p.Id, objectId);
                var result = await _products.DeleteOneAsync(filter);
                return result.IsAcknowledged && result.DeletedCount > 0;
            }
            return false;
        }

        public Task EnsureSchemaAsync() => Task.CompletedTask;

        public async Task<List<Product>> GetProductsForAdminAsync()
        {
            return await _products.Find(p => p.Title != "Analyzing Product...").SortByDescending(p => p.LastUpdated).ToListAsync();
        }

        public async Task<long> DeleteDetectedAsync()
        {
            var filter = Builders<Product>.Filter.Or(
                Builders<Product>.Filter.Regex(p => p.Title, new BsonRegularExpression("Detected|Analyzing", "i")),
                Builders<Product>.Filter.Eq(p => p.Title, "") | Builders<Product>.Filter.Eq(p => p.Title, null)
            );
            var result = await _products.DeleteManyAsync(filter);
            return result.DeletedCount;
        }

        public async Task<IEnumerable<EmbedModel>> GetAllVideosAsync() => await _videos.Find(_ => true).ToListAsync();

        public async Task AddVideoAsync(EmbedModel video) => await _videos.InsertOneAsync(video);

        public async Task DeleteVideoAsync(string id)
        {
            // এখানে "Id" স্ট্রিং হিসেবে পাস করা হয়েছে যাতে টাইপ কনফ্লিক্ট না হয়
            var filter = Builders<EmbedModel>.Filter.Eq("Id", id);
            await _videos.DeleteOneAsync(filter);
        }
    }
}
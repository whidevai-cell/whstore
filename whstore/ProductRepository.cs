using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using whstore.Models;

namespace whstore.Services
{
    public class ProductRepository : IProductRepository
    {
        private readonly ConcurrentDictionary<int, ProductModel> _store = new();
        private int _nextId = 0;

        public Task<IEnumerable<ProductModel>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<ProductModel>>(_store.Values.ToArray());
        }

        public Task<List<ProductModel>> GetActiveAsync(string? search = null)
        {
            var query = _store.Values.Where(p => p.IsActive);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(p => (p.Title?.ToLower().Contains(s) ?? false));
            }
            return Task.FromResult(query.ToList());
        }

        public Task<ProductModel?> GetByIdAsync(int id)
        {
            _store.TryGetValue(id, out var model);
            return Task.FromResult(model);
        }

        public Task AddAsync(ProductModel product)
        {
            product.Id = Interlocked.Increment(ref _nextId);
            _store[product.Id] = product;
            return Task.CompletedTask;
        }

        public Task<bool> UpdateAsync(ProductModel product)
        {
            if (product == null || product.Id <= 0) return Task.FromResult(false);
            _store.AddOrUpdate(product.Id, product, (_, __) => product);
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(int id)
        {
            return Task.FromResult(_store.TryRemove(id, out _));
        }

        public Task EnsureSchemaAsync() => Task.CompletedTask;
    }
}
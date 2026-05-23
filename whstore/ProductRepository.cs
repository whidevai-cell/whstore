using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using whstore.Models;

namespace whstore.Services // 👈 এই লাইনটি নিশ্চিত করবে যে Program.cs একে খুঁজে পাবে!
{
    public class ProductRepository : IProductRepository
    {
        private readonly ConcurrentDictionary<int, ProductModel> _store = new();
        private int _nextId;

        public Task<IEnumerable<ProductModel>> GetAllAsync()
        {
            var snapshot = _store.Values.ToArray();
            return Task.FromResult<IEnumerable<ProductModel>>(snapshot);
        }

        public Task<ProductModel?> GetByIdAsync(int id)
        {
            _store.TryGetValue(id, out var model);
            return Task.FromResult(model);
        }

        public Task AddAsync(ProductModel product)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));

            var id = Interlocked.Increment(ref _nextId);
            product.Id = id;
            _store[id] = product;
            return Task.CompletedTask;
        }

        public Task<bool> UpdateAsync(ProductModel product)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));
            if (product.Id <= 0) return Task.FromResult(false);

            _store.AddOrUpdate(product.Id, product, (_, __) => product);
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(int id)
        {
            return Task.FromResult(_store.TryRemove(id, out _));
        }
    }
}
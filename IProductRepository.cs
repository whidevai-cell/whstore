using System.Collections.Generic;
using System.Threading.Tasks;
using whstore.Models;

namespace whstore.Services
{
    public interface IProductRepository
    {
        Task<List<ProductModel>> GetActiveAsync(string? search = null);
        Task<List<ProductModel>> GetAllAsync();
        Task<ProductModel?> GetByIdAsync(int id);
        Task AddAsync(ProductModel product);
        Task EnsureSchemaAsync();
    }
}
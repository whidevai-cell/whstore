using System.Collections.Generic;
using System.Threading.Tasks;
using whstore.Models;

namespace whstore.Services
{
    public interface IProductRepository
    {
        Task<IEnumerable<ProductModel>> GetAllAsync();
        Task<List<ProductModel>> GetActiveAsync(string? search = null);
        Task<ProductModel?> GetByIdAsync(int id);
        Task AddAsync(ProductModel product);
        Task<bool> UpdateAsync(ProductModel product);
        Task<bool> DeleteAsync(int id);
        Task EnsureSchemaAsync();
    }
}
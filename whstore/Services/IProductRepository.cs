using System.Collections.Generic;
using System.Threading.Tasks;
using whstore.Models;

namespace whstore.Services
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<List<Product>> GetActiveAsync(string? search = null);

        // int ?? string ??? ???
        Task<Product?> GetByIdAsync(string id);

        Task AddAsync(Product product);
        Task<bool> UpdateAsync(Product product);

        // int ?? string ??? ???
        Task<bool> DeleteAsync(string id);

        Task EnsureSchemaAsync();
        Task<List<Product>> GetProductsForAdminAsync();
        Task<long> DeleteDetectedAsync();
    }
}
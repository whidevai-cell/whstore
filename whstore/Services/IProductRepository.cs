using System.Collections.Generic;
using System.Threading.Tasks;
using whstore.Models;

namespace whstore.Services
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<IEnumerable<Product>> GetActiveAsync(string searchString); // string? নয়, string হবে
        Task<Product> GetByIdAsync(string id); // Product? নয়, Product হবে
        Task AddAsync(Product product);
        Task<bool> UpdateAsync(Product product);
        Task<bool> DeleteAsync(string id);
        Task EnsureSchemaAsync();
        Task<List<Product>> GetProductsForAdminAsync();
        Task<long> DeleteDetectedAsync();

        // ভিডিও মেথডস
        Task<IEnumerable<EmbedModel>> GetAllVideosAsync();
        Task AddVideoAsync(EmbedModel video);
        Task DeleteVideoAsync(string id);
    }
}
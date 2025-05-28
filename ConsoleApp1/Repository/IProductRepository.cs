// Repository/IProductRepository.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using VinheriaAgnelo.Models;

namespace VinheriaAgnelo.Repository
{
    public interface IProductRepository
    {
        Task<Product> GetByIdAsync(int id);
        Task<List<Product>> GetAllAsync();
        Task<List<Product>> GetByFilterAsync(string category, decimal? minPrice, decimal? maxPrice, string type);
        Task<Product> CreateAsync(Product product);
        Task<Product> UpdateAsync(Product product);
        Task<bool> DeleteAsync(int id);
        Task<int> UpdateStockAsync(int id, int newQuantity);
    }
}
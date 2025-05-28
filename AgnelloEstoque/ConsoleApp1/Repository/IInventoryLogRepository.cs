// Repository/IInventoryLogRepository.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using VinheriaAgnelo.Models;

namespace VinheriaAgnelo.Repository
{
    public interface IInventoryLogRepository
    {
        Task<InventoryLog> CreateAsync(InventoryLog log);
        Task<List<InventoryLog>> GetByProductIdAsync(int productId);
        Task<List<InventoryLog>> GetByTypeAsync(InventoryLogType type);
        Task<List<InventoryLog>> GetByReasonContainingAsync(string searchText);
    }
}
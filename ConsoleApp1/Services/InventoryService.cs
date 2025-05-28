using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VinheriaAgnelo.Models;
using VinheriaAgnelo.Repository;

namespace VinheriaAgnelo.Services
{
    public class InventoryService
    {
        private readonly IProductRepository _productRepository;
        private readonly IInventoryLogRepository _inventoryLogRepository;

        public InventoryService(
            IProductRepository productRepository,
            IInventoryLogRepository inventoryLogRepository)
        {
            _productRepository = productRepository;
            _inventoryLogRepository = inventoryLogRepository;
        }

        public async Task<bool> CheckAvailabilityAsync(int productId, int requestedQuantity)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            return product != null && product.StockQuantity >= requestedQuantity;
        }

        public async Task<InventoryOperationResult> AddToInventoryAsync(
            int productId, int quantity, string reason, string userId)
        {
            if (quantity <= 0)
                return new InventoryOperationResult { Success = false, Message = "Quantidade deve ser maior que zero" };

            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
                return new InventoryOperationResult { Success = false, Message = "Produto não encontrado" };

            product.StockQuantity += quantity;
            await _productRepository.UpdateAsync(product);

            // Log the inventory change
            await _inventoryLogRepository.CreateAsync(new InventoryLog
            {
                ProductId = productId,
                Quantity = quantity,
                Type = InventoryLogType.Addition,
                Reason = reason,
                UserId = userId,
                Timestamp = DateTime.Now
            });

            return new InventoryOperationResult 
            { 
                Success = true, 
                Message = $"{quantity} unidades adicionadas ao estoque do produto {product.Name}" 
            };
        }

        public async Task<InventoryOperationResult> RemoveFromInventoryAsync(
            int productId, int quantity, string reason, string userId)
        {
            if (quantity <= 0)
                return new InventoryOperationResult { Success = false, Message = "Quantidade deve ser maior que zero" };

            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
                return new InventoryOperationResult { Success = false, Message = "Produto não encontrado" };

            if (product.StockQuantity < quantity)
                return new InventoryOperationResult { Success = false, Message = "Estoque insuficiente" };

            product.StockQuantity -= quantity;
            await _productRepository.UpdateAsync(product);

            // Log the inventory change
            await _inventoryLogRepository.CreateAsync(new InventoryLog
            {
                ProductId = productId,
                Quantity = -quantity,
                Type = InventoryLogType.Removal,
                Reason = reason,
                UserId = userId,
                Timestamp = DateTime.Now
            });

            return new InventoryOperationResult 
            { 
                Success = true, 
                Message = $"{quantity} unidades removidas do estoque do produto {product.Name}" 
            };
        }

        public async Task<List<Product>> GetLowStockProductsAsync(int threshold = 10)
        {
            var products = await _productRepository.GetAllAsync();
            return products.Where(p => p.StockQuantity <= threshold).ToList();
        }

        public async Task<InventoryOperationResult> ReserveStockAsync(int productId, int quantity, string orderId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
                return new InventoryOperationResult { Success = false, Message = "Produto não encontrado" };

            if (product.StockQuantity < quantity)
                return new InventoryOperationResult { Success = false, Message = "Estoque insuficiente" };

            // Don't actually remove from inventory until order is confirmed
            // Just create a reservation record
            await _inventoryLogRepository.CreateAsync(new InventoryLog
            {
                ProductId = productId,
                Quantity = quantity,
                Type = InventoryLogType.Reserved,
                Reason = $"Reserva para pedido {orderId}",
                UserId = orderId,
                Timestamp = DateTime.Now
            });

            return new InventoryOperationResult
            {
                Success = true,
                Message = $"{quantity} unidades reservadas do produto {product.Name} para o pedido {orderId}"
            };
        }

        public async Task<InventoryOperationResult> CompleteOrderAsync(string orderId)
        {
            // Find all reserved items for this order
            var reservations = await _inventoryLogRepository.GetByReasonContainingAsync($"Reserva para pedido {orderId}");
            
            foreach (var reservation in reservations)
            {
                var product = await _productRepository.GetByIdAsync(reservation.ProductId);
                if (product != null)
                {
                    product.StockQuantity -= reservation.Quantity;
                    await _productRepository.UpdateAsync(product);

                    // Create a completed inventory log
                    await _inventoryLogRepository.CreateAsync(new InventoryLog
                    {
                        ProductId = reservation.ProductId,
                        Quantity = -reservation.Quantity,
                        Type = InventoryLogType.Sale,
                        Reason = $"Venda confirmada para pedido {orderId}",
                        UserId = orderId,
                        Timestamp = DateTime.Now
                    });
                }
            }

            return new InventoryOperationResult
            {
                Success = true,
                Message = $"Estoque atualizado para o pedido {orderId}"
            };
        }

        public async Task<InventoryOperationResult> ProcessProductReturnAsync(
            int productId, int quantity, string reason, string userId)
        {
            if (quantity <= 0)
                return new InventoryOperationResult { Success = false, Message = "Quantidade deve ser maior que zero" };

            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
                return new InventoryOperationResult { Success = false, Message = "Produto não encontrado" };

            // Add the returned quantity back to inventory
            product.StockQuantity += quantity;
            await _productRepository.UpdateAsync(product);

            // Log the return
            await _inventoryLogRepository.CreateAsync(new InventoryLog
            {
                ProductId = productId,
                Quantity = quantity,
                Type = InventoryLogType.Return,
                Reason = reason,
                UserId = userId,
                Timestamp = DateTime.Now
            });

            return new InventoryOperationResult 
            { 
                Success = true, 
                Message = $"{quantity} unidades retornadas ao estoque do produto {product.Name}" 
            };
        }

        public async Task<InventoryOperationResult> AdjustInventoryAsync(
            int productId, int newQuantity, string reason, string userId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
                return new InventoryOperationResult { Success = false, Message = "Produto não encontrado" };

            int adjustmentQuantity = newQuantity - product.StockQuantity;
            
            // Update the product quantity
            product.StockQuantity = newQuantity;
            await _productRepository.UpdateAsync(product);
            
            // Log the adjustment
            await _inventoryLogRepository.CreateAsync(new InventoryLog
            {
                ProductId = productId,
                Quantity = adjustmentQuantity,
                Type = InventoryLogType.Adjustment,
                Reason = reason,
                UserId = userId,
                Timestamp = DateTime.Now
            });
            
            string direction = adjustmentQuantity >= 0 ? "aumentado" : "reduzido";
            return new InventoryOperationResult
            {
                Success = true,
                Message = $"Estoque do produto {product.Name} {direction} para {newQuantity} unidades"
            };
        }

        public async Task<InventoryReport> GenerateInventoryReportAsync()
        {
            var products = await _productRepository.GetAllAsync();
            
            return new InventoryReport
            {
                TotalProducts = products.Count,
                TotalItems = products.Sum(p => p.StockQuantity),
                OutOfStockProducts = products.Count(p => p.StockQuantity == 0),
                LowStockProducts = products.Count(p => p.StockQuantity > 0 && p.StockQuantity <= 10),
                InventoryValue = products.Sum(p => p.Price * p.StockQuantity),
                GeneratedAt = DateTime.Now
            };
        }

        public async Task<List<InventoryLog>> GetProductInventoryHistoryAsync(int productId)
        {
            return await _inventoryLogRepository.GetByProductIdAsync(productId);
        }

        public async Task<Dictionary<string, List<Product>>> GetInventoryStatusReportAsync(int lowStockThreshold = 10)
        {
            var products = await _productRepository.GetAllAsync();
            
            return new Dictionary<string, List<Product>>
            {
                ["OutOfStock"] = products.Where(p => p.StockQuantity == 0).ToList(),
                ["LowStock"] = products.Where(p => p.StockQuantity > 0 && p.StockQuantity <= lowStockThreshold).ToList(),
                ["HealthyStock"] = products.Where(p => p.StockQuantity > lowStockThreshold).ToList()
            };
        }
    }

    public class InventoryOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class InventoryReport
    {
        public int TotalProducts { get; set; }
        public int TotalItems { get; set; }
        public int OutOfStockProducts { get; set; }
        public int LowStockProducts { get; set; }
        public decimal InventoryValue { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
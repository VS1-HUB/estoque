// See https://aka.ms/new-console-template for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VinheriaAgnelo.Models;
using VinheriaAgnelo.Repository;
using VinheriaAgnelo.Services;

namespace VinheriaAgnelo
{
    public class Program
    {
        public class DummyProductRepository : IProductRepository
        {
            private readonly List<Product> _products = new List<Product>
            {
                new Product { Id = 1, Name = "Vinho Tinto Suave", Price = 50, StockQuantity = 10, Category = "Tinto", Type = "Suave", Year = 2020, Region = "Serra Gaúcha" },
                new Product { Id = 2, Name = "Vinho Branco Seco", Price = 75, StockQuantity = 5, Category = "Branco", Type = "Seco", Year = 2021, Region = "Vale dos Vinhedos" }
            };

            public Task<Product> GetByIdAsync(int id)
            {
                var product = _products.FirstOrDefault(p => p.Id == id);
                return Task.FromResult(product ?? new Product()); // Return empty product instead of null
            }

            public Task<List<Product>> GetAllAsync()
            {
                return Task.FromResult(_products);
            }

            public Task<List<Product>> GetByFilterAsync(string category, decimal? minPrice, decimal? maxPrice, string type)
            {
                var query = _products.AsQueryable();
                
                if (!string.IsNullOrEmpty(category))
                    query = query.Where(p => p.Category == category);
                
                if (minPrice.HasValue)
                    query = query.Where(p => p.Price >= minPrice.Value);
                
                if (maxPrice.HasValue)
                    query = query.Where(p => p.Price <= maxPrice.Value);
                
                if (!string.IsNullOrEmpty(type))
                    query = query.Where(p => p.Type == type);
                
                return Task.FromResult(query.ToList());
            }

            public Task<Product> CreateAsync(Product product)
            {
                var newId = _products.Count > 0 ? _products.Max(p => p.Id) + 1 : 1;
                product.Id = newId;
                _products.Add(product);
                return Task.FromResult(product);
            }

            public Task<Product> UpdateAsync(Product product)
            {
                var existingProduct = _products.FirstOrDefault(p => p.Id == product.Id);
                if (existingProduct != null)
                {
                    existingProduct.Name = product.Name;
                    existingProduct.Price = product.Price;
                    existingProduct.StockQuantity = product.StockQuantity;
                    existingProduct.Category = product.Category;
                    existingProduct.Type = product.Type;
                    existingProduct.Year = product.Year;
                    existingProduct.Region = product.Region;
                    return Task.FromResult(existingProduct);
                }
                return Task.FromResult(new Product()); // Return empty product if not found
            }

            public Task<bool> DeleteAsync(int id)
            {
                var product = _products.FirstOrDefault(p => p.Id == id);
                if (product != null)
                {
                    _products.Remove(product);
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }

            public Task<int> UpdateStockAsync(int id, int newQuantity)
            {
                var product = _products.FirstOrDefault(p => p.Id == id);
                if (product != null)
                {
                    product.StockQuantity = newQuantity;
                    return Task.FromResult(newQuantity);
                }
                return Task.FromResult(0);
            }
        }

        public class DummyInventoryLogRepository : IInventoryLogRepository
        {
            private readonly List<InventoryLog> _logs = new List<InventoryLog>();

            public Task<InventoryLog> CreateAsync(InventoryLog log)
            {
                // Assign an ID if needed
                if (log.Id <= 0)
                {
                    log.Id = _logs.Count > 0 ? _logs.Max(l => l.Id) + 1 : 1;
                }
                
                _logs.Add(log);
                Console.WriteLine($"Inventory Log: ProductId={log.ProductId}, Quantity={log.Quantity}, Type={log.Type}, Timestamp={log.Timestamp}");
                return Task.FromResult(log);
            }

            public Task<List<InventoryLog>> GetByProductIdAsync(int productId)
            {
                return Task.FromResult(_logs.Where(l => l.ProductId == productId).ToList());
            }

            public Task<List<InventoryLog>> GetByTypeAsync(InventoryLogType type)
            {
                return Task.FromResult(_logs.Where(l => l.Type == type).ToList());
            }

            public Task<List<InventoryLog>> GetByReasonContainingAsync(string searchText)
            {
                return Task.FromResult(_logs.Where(l => l.Reason != null && 
                    l.Reason.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList());
            }
        }

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Iniciando Sistema de Estoque da Vinheria Agnelo...");

            IProductRepository productRepository = new DummyProductRepository();
            IInventoryLogRepository inventoryLogRepository = new DummyInventoryLogRepository();

            var inventoryService = new InventoryService(productRepository, inventoryLogRepository);
            var cartService = new CartService(inventoryService, productRepository);

            Console.WriteLine("\nVerificando disponibilidade do Produto ID 1...");
            bool isAvailable = await inventoryService.CheckAvailabilityAsync(1, 5);
            Console.WriteLine($"Produto ID 1 está disponível para 5 unidades? {isAvailable}");

            if (isAvailable)
            {
                Console.WriteLine("\nAdicionando Produto ID 1 ao carrinho com reserva...");
                var cart = new VinheriaAgnelo.Models.Cart { Id = Guid.NewGuid() }; 
                var user = "testUser"; 
                bool addedToCart = await cartService.AddToCartWithReservationAsync(cart, 1, 2, user);
                Console.WriteLine($"Produto ID 1 adicionado ao carrinho com reserva? {addedToCart}");

                if(addedToCart)
                {
                    var completeOrderResult = await inventoryService.CompleteOrderAsync(cart.Id.ToString());
                    Console.WriteLine($"Conclusão do pedido {cart.Id}: {completeOrderResult.Message}");
                }
            }
            
            Console.WriteLine("\nVerificando status do estoque...");
            var inventoryStatus = await inventoryService.GetInventoryStatusReportAsync(lowStockThreshold: 3);
            Console.WriteLine($"Produtos sem estoque: {inventoryStatus["OutOfStock"].Count}");
            Console.WriteLine($"Produtos com baixo estoque (<=3): {inventoryStatus["LowStock"].Count}");
            foreach(var product in inventoryStatus["LowStock"])
            {
                Console.WriteLine($"  - {product.Name}: {product.StockQuantity} unidades");
            }
            Console.WriteLine($"Produtos com estoque saudável (>3): {inventoryStatus["HealthyStock"].Count}");


            Console.WriteLine("\nExemplo de ajuste de estoque para Produto ID 2...");
            var adjustmentResult = await inventoryService.AdjustInventoryAsync(2, 15, "Ajuste manual", "adminUser");
            Console.WriteLine(adjustmentResult.Message);

            var product2 = await productRepository.GetByIdAsync(2);
            Console.WriteLine($"Novo estoque do Produto ID 2: {product2?.StockQuantity}");

            Console.WriteLine("\nSistema finalizado.");
        }
    }
}

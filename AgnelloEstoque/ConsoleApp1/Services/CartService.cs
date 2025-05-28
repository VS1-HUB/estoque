using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VinheriaAgnelo.Models;
using VinheriaAgnelo.Repository;

namespace VinheriaAgnelo.Services
{
    public class CartService
    {
        private readonly InventoryService _inventoryService;
        private readonly IProductRepository _productRepository;
        // In-memory storage for cart items until we have a database
        private readonly Dictionary<Guid, Cart> _carts = new Dictionary<Guid, Cart>();

        public CartService(InventoryService inventoryService, IProductRepository productRepository)
        {
            _inventoryService = inventoryService;
            _productRepository = productRepository;
        }

        public Cart GetOrCreateCart(string userId)
        {
            var existingCart = _carts.Values
                .FirstOrDefault(c => c.UserId == userId && c.Items.Count > 0);

            if (existingCart != null)
                return existingCart;

            var newCart = new Cart 
            { 
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = DateTime.Now
            };
            
            _carts[newCart.Id] = newCart;
            return newCart;
        }

        public Cart GetCart(Guid cartId)
        {
            return _carts.TryGetValue(cartId, out var cart) ? cart : null;
        }

        public async Task<bool> AddToCartAsync(Cart cart, int productId, int quantity)
        {
            // Check inventory availability first (RN02.1)
            var isAvailable = await _inventoryService.CheckAvailabilityAsync(productId, quantity);
            
            if (!isAvailable)
                return false;
            
            // Get product details
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null || product.Id <= 0)
                return false;
                
            // Add to cart
            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (existingItem != null)
            {
                // Update existing item
                existingItem.Quantity += quantity;
            }
            else
            {
                // Add new item
                cart.Items.Add(new CartItem
                {
                    Id = cart.Items.Count > 0 ? cart.Items.Max(i => i.Id) + 1 : 1,
                    CartId = cart.Id,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.IsOnSale ? product.PromotionalPrice.Value : product.Price,
                    Quantity = quantity,
                    DateAdded = DateTime.Now
                });
            }
            
            cart.UpdatedAt = DateTime.Now;
            _carts[cart.Id] = cart;
            
            return true;
        }

        public async Task<bool> AddToCartWithReservationAsync(Cart cart, int productId, int quantity, string userId)
        {
            // First check availability
            var isAvailable = await _inventoryService.CheckAvailabilityAsync(productId, quantity);
            
            if (!isAvailable)
                return false;
            
            // Reserve the stock for this cart/session
            var reservationResult = await _inventoryService.ReserveStockAsync(
                productId, 
                quantity, 
                cart.Id.ToString()
            );
            
            if (!reservationResult.Success)
                return false;
            
            // Get product details
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null || product.Id <= 0)
                return false;
                
            // Add to cart
            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (existingItem != null)
            {
                // Update existing item
                existingItem.Quantity += quantity;
            }
            else
            {
                // Add new item
                cart.Items.Add(new CartItem
                {
                    Id = cart.Items.Count > 0 ? cart.Items.Max(i => i.Id) + 1 : 1,
                    CartId = cart.Id,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.IsOnSale ? product.PromotionalPrice.Value : product.Price,
                    Quantity = quantity,
                    DateAdded = DateTime.Now
                });
            }
            
            cart.UserId = userId;
            cart.UpdatedAt = DateTime.Now;
            _carts[cart.Id] = cart;
            
            return true;
        }

        public async Task<bool> UpdateCartItemQuantityAsync(Cart cart, int productId, int newQuantity)
        {
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null)
                return false;

            if (newQuantity <= 0)
            {
                // Remove the item if quantity is zero or negative
                return RemoveCartItem(cart, productId);
            }

            // Check if we have enough inventory for the new quantity
            var isAvailable = await _inventoryService.CheckAvailabilityAsync(productId, newQuantity);
            if (!isAvailable)
                return false;

            // Update the reservation
            int quantityDifference = newQuantity - item.Quantity;
            if (quantityDifference != 0)
            {
                var result = quantityDifference > 0 
                    ? await _inventoryService.ReserveStockAsync(productId, quantityDifference, cart.Id.ToString())
                    : new InventoryOperationResult { Success = true }; // No need to reserve if reducing

                if (!result.Success)
                    return false;
            }

            // Update quantity
            item.Quantity = newQuantity;
            cart.UpdatedAt = DateTime.Now;
            _carts[cart.Id] = cart;
            
            return true;
        }

        public bool RemoveCartItem(Cart cart, int productId)
        {
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null)
                return false;

            // Remove the item
            cart.Items.Remove(item);
            cart.UpdatedAt = DateTime.Now;
            _carts[cart.Id] = cart;
            
            // Note: This doesn't automatically release the reservation
            // You might want to add that logic or handle it separately
            
            return true;
        }

        public void ClearCart(Cart cart)
        {
            cart.Items.Clear();
            cart.UpdatedAt = DateTime.Now;
            _carts[cart.Id] = cart;
            
            // Note: This doesn't automatically release the reservations
            // You might want to add that logic or handle it separately
        }

        public async Task<bool> FinalizeCartAsync(Cart cart)
        {
            if (cart.IsEmpty)
                return false;

            // Complete the order which will convert reservations to actual inventory changes
            var orderResult = await _inventoryService.CompleteOrderAsync(cart.Id.ToString());
            
            if (!orderResult.Success)
                return false;
            
            // In a real application, you would create an Order record here
            // and possibly clear the cart or mark it as processed
            
            ClearCart(cart);
            return true;
        }

        public async Task<bool> AbandonCartAsync(Cart cart)
        {
            if (cart.IsEmpty)
                return true;

            // In a real application, you would need to release all reservations
            // This would require additional inventory service methods
            
            ClearCart(cart);
            return true;
        }
    }
}
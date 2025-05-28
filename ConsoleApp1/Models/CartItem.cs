using System;

namespace VinheriaAgnelo.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public Guid CartId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public DateTime DateAdded { get; set; }
        
        public decimal Subtotal => UnitPrice * Quantity;
    }
}
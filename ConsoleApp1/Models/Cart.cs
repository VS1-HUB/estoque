using System;
using System.Collections.Generic;
using System.Linq;

namespace VinheriaAgnelo.Models
{
    public class Cart
    {
        public Guid Id { get; set; }
        public List<CartItem> Items { get; set; } = new List<CartItem>();
        public string UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        public decimal TotalAmount => Items.Sum(item => item.Subtotal);
        public int TotalItems => Items.Sum(item => item.Quantity);
        public bool IsEmpty => !Items.Any();
    }
}
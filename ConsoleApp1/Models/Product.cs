using System;
using System.Collections.Generic;

namespace VinheriaAgnelo.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public decimal? PromotionalPrice { get; set; }
        public int StockQuantity { get; set; }
        public bool IsAvailable => StockQuantity > 0;
        public string Category { get; set; }
        public string Type { get; set; }
        public int Year { get; set; }
        public string Region { get; set; }
        public string PairingNotes { get; set; }
        public double Rating { get; set; }
        public List<ProductReview> Reviews { get; set; } = new List<ProductReview>();

        public bool IsOnSale => PromotionalPrice.HasValue && PromotionalPrice < Price;
    }

    public class ProductReview
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int Rating { get; set; } // 1-5 stars
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsApproved { get; set; }
    }
}
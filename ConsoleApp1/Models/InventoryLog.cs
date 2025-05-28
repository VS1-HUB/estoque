using System;

namespace VinheriaAgnelo.Models
{
    public class InventoryLog
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public InventoryLogType Type { get; set; }
        public string Reason { get; set; }
        public string UserId { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum InventoryLogType
    {
        Addition,
        Removal,
        Reserved,
        Sale,
        Adjustment,
        Return
    }
}
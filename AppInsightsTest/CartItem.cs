using System;

namespace AppInsightsTest
{
    public class CartItem
    {
        public int ItemId { get; init; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public decimal Cost { get; set; }
    }
}
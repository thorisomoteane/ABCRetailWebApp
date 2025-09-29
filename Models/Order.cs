using Azure;
using Azure.Data.Tables;
using System;

namespace ABCRetailWebApp.Models
{
    public class Order : ITableEntity
    {
        public string PartitionKey { get; set; } = "Orders";
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string OrderId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }

        // Price needs to be double for Azure Tables (decimal isn't supported)
        public double Price { get; set; }

        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = "Pending";

        // Add computed property for display
        public double TotalPrice => Price * Quantity;
    }
}
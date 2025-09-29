namespace ABCRetailWebApp.Models
{
    public class TransactionMessage
    {
        public string OrderId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
namespace ABCRetailWebApp.Models
{
    public class Product
    {
        public string ProductId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public IFormFile? ImageFile { get; set; }
        public string? ImageUrl { get; set; }
    }
}
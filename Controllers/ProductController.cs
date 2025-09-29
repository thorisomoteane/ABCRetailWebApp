using Microsoft.AspNetCore.Mvc;
using ABCRetailWebApp.Models;
using ABCRetailWebApp.Services;

namespace ABCRetailWebApp.Controllers
{
    public class ProductController : Controller
    {
        private readonly AzureStorageService _storageService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(AzureStorageService storageService, ILogger<ProductController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(Product product)
        {
            if (product.ImageFile != null && product.ImageFile.Length > 0)
            {
                product.ProductId = Guid.NewGuid().ToString();
                var imageUrl = await _storageService.UploadProductImageAsync(product.ImageFile, product.ProductId);

                if (!string.IsNullOrEmpty(imageUrl))
                {
                    product.ImageUrl = imageUrl;
                    TempData["Success"] = $"Product image uploaded successfully! URL: {imageUrl}";
                    return View("UploadSuccess", product);
                }

                TempData["Error"] = "Failed to upload product image.";
            }

            return View(product);
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using ABCRetailWebApp.Services;
using System.Text;

namespace ABCRetailWebApp.Controllers
{
    public class ReportController : Controller
    {
        private readonly AzureStorageService _storageService;
        private readonly ILogger<ReportController> _logger;

        public ReportController(AzureStorageService storageService, ILogger<ReportController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        public IActionResult Generate()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Generate(string reportType)
        {
            var orders = await _storageService.GetAllOrdersAsync();

            var reportBuilder = new StringBuilder();
            reportBuilder.AppendLine($"ABC Retail {reportType} Report");
            reportBuilder.AppendLine($"Generated: {DateTime.Now}");
            reportBuilder.AppendLine("=".PadRight(50, '='));
            reportBuilder.AppendLine();

            reportBuilder.AppendLine("Order ID,Customer,Product,Quantity,Price,Date,Status");

            foreach (var order in orders)
            {
                reportBuilder.AppendLine($"{order.OrderId},{order.CustomerName},{order.ProductName},{order.Quantity},{order.Price},{order.OrderDate},{order.Status}");
            }

            reportBuilder.AppendLine();
            reportBuilder.AppendLine($"Total Orders: {orders.Count}");
            reportBuilder.AppendLine($"Total Revenue: ${orders.Sum(o => o.Price * o.Quantity):F2}");

            var fileName = $"{reportType}_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var saved = await _storageService.SaveReportToFileShareAsync(reportBuilder.ToString(), fileName);

            if (saved)
            {
                TempData["Success"] = $"Report generated and saved: {fileName}";
                ViewBag.ReportContent = reportBuilder.ToString();
                ViewBag.FileName = fileName;
            }
            else
            {
                TempData["Error"] = "Failed to generate report.";
            }

            return View("ReportResult");
        }

        public async Task<IActionResult> Download(string fileName)
        {
            var content = await _storageService.DownloadReportFromFileShareAsync(fileName);

            if (!string.IsNullOrEmpty(content))
            {
                var bytes = Encoding.UTF8.GetBytes(content);
                return File(bytes, "text/csv", fileName);
            }

            TempData["Error"] = "Report not found.";
            return RedirectToAction("Generate");
        }
    }
}
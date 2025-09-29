using Microsoft.AspNetCore.Mvc;
using ABCRetailWebApp.Models;
using ABCRetailWebApp.Services;

namespace ABCRetailWebApp.Controllers
{
    public class OrderController : Controller
    {
        private readonly AzureStorageService _storageService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(AzureStorageService storageService, ILogger<OrderController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _storageService.GetAllOrdersAsync();
            return View(orders);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Order order)
        {
            if (ModelState.IsValid)
            {
                order.OrderId = Guid.NewGuid().ToString();
                order.OrderDate = DateTime.UtcNow;
                order.Status = "Pending";
                order.RowKey = order.OrderId;
                order.PartitionKey = "Orders";

                // Ensure price is set properly (convert if needed)
                // Price is already set from form binding

                // Save to Azure Table
                var saved = await _storageService.SaveOrderToTableAsync(order);

                if (saved)
                {
                    // Send message to queue for processing
                    await _storageService.SendTransactionMessageAsync(order.OrderId, "ProcessPayment");

                    TempData["Success"] = $"Order {order.OrderId} created successfully!";
                    return RedirectToAction("Index");
                }

                TempData["Error"] = "Failed to create order.";
            }

            return View(order);
        }

        public async Task<IActionResult> ProcessQueue()
        {
            var messages = await _storageService.ReceiveTransactionMessagesAsync();
            ViewBag.Messages = messages;
            return View();
        }
    }
}
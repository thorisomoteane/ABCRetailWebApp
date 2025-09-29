using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;
using Azure.Storage.Queues;
using ABCRetailWebApp.Models;
using System.Text.Json;

namespace ABCRetailWebApp.Services
{
    public class AzureStorageService
    {
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureStorageService> _logger;

        public AzureStorageService(IConfiguration configuration, ILogger<AzureStorageService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _connectionString = configuration["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException("Azure Storage connection string not found");
        }

        // 1. Azure Table Storage - Store Order Information
        public async Task<bool> SaveOrderToTableAsync(Order order)
        {
            try
            {
                var tableClient = new TableClient(_connectionString, _configuration["AzureStorage:TableName"]);
                await tableClient.CreateIfNotExistsAsync();

                order.RowKey = order.OrderId;
                order.PartitionKey = "Orders";

                await tableClient.AddEntityAsync(order);
                _logger.LogInformation($"Order {order.OrderId} saved to Azure Table");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving order to table");
                return false;
            }
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            try
            {
                var tableClient = new TableClient(_connectionString, _configuration["AzureStorage:TableName"]);
                await tableClient.CreateIfNotExistsAsync();

                var orders = new List<Order>();
                await foreach (var order in tableClient.QueryAsync<Order>())
                {
                    orders.Add(order);
                }
                return orders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders from table");
                return new List<Order>();
            }
        }

        // 2. Azure Blob Storage - Store Product Images
        public async Task<string?> UploadProductImageAsync(IFormFile imageFile, string productId)
        {
            try
            {
                var containerClient = new BlobContainerClient(_connectionString, _configuration["AzureStorage:ContainerName"]);
                await containerClient.CreateIfNotExistsAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob);

                var fileName = $"{productId}_{imageFile.FileName}";
                var blobClient = containerClient.GetBlobClient(fileName);

                using var stream = imageFile.OpenReadStream();
                await blobClient.UploadAsync(stream, overwrite: true);

                _logger.LogInformation($"Image uploaded for product {productId}");
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image to blob storage");
                return null;
            }
        }

        // 3. Azure Queue Storage - Transaction Processing
        public async Task<bool> SendTransactionMessageAsync(string orderId, string action)
        {
            try
            {
                var queueClient = new QueueClient(_connectionString, _configuration["AzureStorage:QueueName"]);
                await queueClient.CreateIfNotExistsAsync();

                var message = new TransactionMessage
                {
                    OrderId = orderId,
                    Action = action,
                    Timestamp = DateTime.UtcNow
                };

                var messageJson = JsonSerializer.Serialize(message);
                await queueClient.SendMessageAsync(messageJson);

                _logger.LogInformation($"Transaction message sent for order {orderId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to queue");
                return false;
            }
        }

        public async Task<List<string>> ReceiveTransactionMessagesAsync(int maxMessages = 5)
        {
            try
            {
                var queueClient = new QueueClient(_connectionString, _configuration["AzureStorage:QueueName"]);
                await queueClient.CreateIfNotExistsAsync();

                var messages = new List<string>();
                var response = await queueClient.ReceiveMessagesAsync(maxMessages);

                foreach (var message in response.Value)
                {
                    messages.Add(message.MessageText);
                    // Delete message after processing
                    await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
                }

                return messages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving messages from queue");
                return new List<string>();
            }
        }

        // 4. Azure Files - Store Reports
        public async Task<bool> SaveReportToFileShareAsync(string reportContent, string fileName)
        {
            try
            {
                var shareClient = new ShareClient(_connectionString, _configuration["AzureStorage:FileShareName"]);
                await shareClient.CreateIfNotExistsAsync();

                var directoryClient = shareClient.GetRootDirectoryClient();
                var fileClient = directoryClient.GetFileClient(fileName);

                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(reportContent));
                await fileClient.CreateAsync(stream.Length);
                await fileClient.UploadAsync(stream);

                _logger.LogInformation($"Report {fileName} saved to Azure Files");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving report to file share");
                return false;
            }
        }

        public async Task<string?> DownloadReportFromFileShareAsync(string fileName)
        {
            try
            {
                var shareClient = new ShareClient(_connectionString, _configuration["AzureStorage:FileShareName"]);
                var directoryClient = shareClient.GetRootDirectoryClient();
                var fileClient = directoryClient.GetFileClient(fileName);

                if (await fileClient.ExistsAsync())
                {
                    var download = await fileClient.DownloadAsync();
                    using var reader = new StreamReader(download.Value.Content);
                    return await reader.ReadToEndAsync();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading report from file share");
                return null;
            }
        }
    }
}
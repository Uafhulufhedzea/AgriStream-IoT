using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using AgriStream_IoT.Models;

namespace AgriStream_IoT.Services
{
    public class AgriStorageService : IAgriStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly TableServiceClient _tableServiceClient;
        private readonly ILogger<AgriStorageService> _logger;

        public AgriStorageService(IConfiguration configuration, ILogger<AgriStorageService> logger)
        {
            string connString = configuration.GetConnectionString("AzureStorage")
                ?? configuration["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException("Azure Storage connection string is missing.");

            _blobServiceClient = new BlobServiceClient(connString);
            _tableServiceClient = new TableServiceClient(connString);
            _logger = logger;
        }

        public async Task<string> UploadDroneImageAsync(IFormFile file)
        {
            if (file.Length == 0 || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Please select a non-empty image file.", nameof(file));
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient("drone-images");
            await containerClient.CreateIfNotExistsAsync();

            string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var blobClient = containerClient.GetBlobClient(uniqueFileName);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, true);
            }

            _logger.LogInformation("Uploaded image {BlobName} to the drone-images container.", uniqueFileName);
            return blobClient.Uri.ToString();
        }

        public async Task SaveTelemetryAsync(SensorReading reading)
        {
            var tableClient = _tableServiceClient.GetTableClient("SensorTelemetry");
            await tableClient.CreateIfNotExistsAsync();

            reading.PartitionKey = "SoilSensors";
            reading.RowKey = $"{DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks}_{Guid.NewGuid():N}";
            reading.Timestamp = DateTimeOffset.UtcNow;

            await tableClient.AddEntityAsync(reading);
            _logger.LogInformation("Saved telemetry for sensor {SensorId} in the SensorTelemetry table with row key {RowKey}.", reading.SensorId, reading.RowKey);
        }
    }
}

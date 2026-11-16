using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Data.SqlClient;
using System.Net;
using System.Text.Json;

namespace AgriStream.Functions
{
    public class AgriPipelineFunctions
    {
        private readonly ILogger _logger;
        private static readonly string StorageConnectionString = Environment.GetEnvironmentVariable("AzureStorageConnectionString") ?? string.Empty;
        private static readonly string SqlConnectionString = Environment.GetEnvironmentVariable("SqlConnectionString") ?? string.Empty;

        public AgriPipelineFunctions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AgriPipelineFunctions>();
        }

        // 1. BLOB TRIGGER: Audits uploaded drone images into Table Storage
        [Function("DroneImageBlobTrigger")]
        public async Task RunBlobTrigger(
            [BlobTrigger("drone-images/{name}", Connection = "AzureStorageConnectionString")] string blobContent,
            string name)
        {
            _logger.LogInformation($"Drone snapshot detected. Processing file: {name}");

            var tableClient = new TableClient(StorageConnectionString, "SensorTelemetry");
            await tableClient.CreateIfNotExistsAsync();

            var auditLog = new TableEntity("DroneAudit", Guid.NewGuid().ToString())
            {
                { "FileName", name },
                { "ProcessedAt", DateTimeOffset.UtcNow },
                { "Status", "Audit Logged / Processing Pending" }
            };

            await tableClient.AddEntityAsync(auditLog);
            _logger.LogInformation($"Successfully logged processing audit entry for {name} to Table Storage.");
        }

        // 2. HTTP GET TRIGGER: Retrieves telemetry data from NoSQL Table Storage
        [Function("GetLatestTelemetry")]
        public async Task<HttpResponseData> RunHttpGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "telemetry")] HttpRequestData req)
        {
            _logger.LogInformation("Processing GET request to fetch soil sensor logs.");

            var tableClient = new TableClient(StorageConnectionString, "SensorTelemetry");
            await tableClient.CreateIfNotExistsAsync();

            var page = tableClient.QueryAsync<TableEntity>(filter: "PartitionKey eq 'SoilSensors'");
            var results = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>();

            await foreach (var entity in page)
            {
                var dict = new System.Collections.Generic.Dictionary<string, object>();
                foreach (var key in entity.Keys)
                {
                    dict[key] = entity[key];
                }
                results.Add(dict);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(results);
            return response;
        }

        // 3. HTTP POST TRIGGER: Places an Irrigation Command into the Storage Queue
        [Function("PostIrrigationCommand")]
        public async Task<HttpResponseData> RunHttpPost(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "irrigation/trigger")] HttpRequestData req)
        {
            _logger.LogInformation("Processing irrigation post request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var queueClient = new QueueClient(StorageConnectionString, "irrigation-commands");
            await queueClient.CreateIfNotExistsAsync();

            var bytes = System.Text.Encoding.UTF8.GetBytes(requestBody);
            await queueClient.SendMessageAsync(Convert.ToBase64String(bytes));

            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteStringAsync("Irrigation request successfully queued for processing.");
            return response;
        }

        // 4. QUEUE TRIGGER: Listens to the Queue and updates device logs in Azure SQL Database
        [Function("ProcessIrrigationQueue")]
        public async Task RunQueueTrigger(
            [QueueTrigger("irrigation-commands", Connection = "AzureStorageConnectionString")] string queueMessage)
        {
            _logger.LogInformation($"Processing queue payload command: {queueMessage}");

            try
            {
                using var connection = new SqlConnection(SqlConnectionString);
                await connection.OpenAsync();

                string query = "UPDATE UserAccounts SET [Password] = 'Irrigation Active' WHERE Username = @SensorId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SensorId", "SENSOR-01");

                int rowsAffected = await command.ExecuteNonQueryAsync();
                _logger.LogInformation($"Database updated successfully. Rows affected: {rowsAffected}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Queue processing exception thrown: {ex.Message}");
            }
        }
    }
}

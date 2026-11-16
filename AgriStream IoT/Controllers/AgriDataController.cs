using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AgriStream_IoT.Models;
using AgriStream_IoT.Services;

namespace AgriStream_IoT.Controllers
{
    public class AgriDataController : Controller
    {
        private readonly IAgriStorageService _storageService;
        private readonly HttpClient _httpClient;

        public AgriDataController(IAgriStorageService storageService)
        {
            _storageService = storageService;
            // ONLY CHANGE: Updated port 7071 to 7011 to match your active host
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:7011/") };
        }

        // GET: AgriData/Dashboard
        public IActionResult Dashboard()
        {
            return View();
        }

        // POST: AgriData/UploadDroneImage
        [HttpPost]
        public async Task<IActionResult> UploadDroneImage(IFormFile droneFile)
        {
            if (droneFile != null && droneFile.Length > 0)
            {
                string fileUrl = await _storageService.UploadDroneImageAsync(droneFile);
                ViewBag.Message = "Drone snapshot uploaded to Azure Blob Storage successfully!";
                ViewBag.ImageUrl = fileUrl;
            }
            return View("Dashboard");
        }

        // POST: AgriData/LogTelemetry
        [HttpPost]
        public async Task<IActionResult> LogTelemetry(SensorReading model)
        {
            if (ModelState.IsValid)
            {
                await _storageService.SaveTelemetryAsync(model);
                ViewBag.TelemetryMessage = "Telemetry packet streamed to Azure Table Storage successfully!";
            }
            return View("Dashboard");
        }

        // POST: AgriData/TriggerIrrigation
        [HttpPost]
        public async Task<IActionResult> TriggerIrrigation(string sensorId, string statusReason)
        {
            var payload = new { SensorId = sensorId, Status = "Triggered", Reason = statusReason };
            var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync("api/irrigation/trigger", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    TempData["CommandStatus"] = "Irrigation instruction securely offloaded to Serverless Ingestion Queue.";
                }
                else
                {
                    TempData["CommandStatus"] = "Edge infrastructure routing failed.";
                }
            }
            catch (Exception ex)
            {
                TempData["CommandStatus"] = $"Connection error: {ex.Message}";
            }

            return RedirectToAction("Dashboard");
        }
    }
}

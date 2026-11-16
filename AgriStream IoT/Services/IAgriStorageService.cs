using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using AgriStream_IoT.Models;

namespace AgriStream_IoT.Services
{
    public interface IAgriStorageService
    {
        Task<string> UploadDroneImageAsync(IFormFile file);
        Task SaveTelemetryAsync(SensorReading reading);
    }
}

using Azure;
using Azure.Data.Tables;
using System;
using System.ComponentModel.DataAnnotations;

namespace AgriStream_IoT.Models
{
    public class SensorReading : ITableEntity
    {
        public string PartitionKey { get; set; } = "SoilSensors";
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [Required, StringLength(100)]
        public string SensorId { get; set; } = string.Empty;

        [Range(0, 100)]
        public double SoilMoisturePercentage { get; set; }

        [Range(-100, 100)]
        public double TemperatureCelsius { get; set; }

        [Required, StringLength(50)]
        public string Status { get; set; } = "Normal";
    }
}

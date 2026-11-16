using System.ComponentModel.DataAnnotations;

namespace AgriStream_IoT.Models
{
    public class IrrigationCommand
    {
        [Required, StringLength(100)]
        public string? FieldId { get; set; }

        public int UserId { get; set; }

        [Required, Range(1, 1440)]
        public int DurationMinutes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Queued, Processing, Completed
    }
}

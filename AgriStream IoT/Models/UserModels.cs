using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace AgriStream_IoT.Models
{
    public class SystemRole
    {
        [Key]
        public int RoleId { get; set; }

        [Required, StringLength(50)]
        public string RoleName { get; set; } // Farmer, Agronomist, Admin

        public ICollection<UserAccount> Users { get; set; }
    }

    public class UserAccount
    {
        [Key]
        public int UserId { get; set; }

        [Required, StringLength(100)]
        public string Username { get; set; }

        [Required]
        [StringLength(255, MinimumLength = 8)]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "Password must be at least 8 characters and contain an uppercase letter, a lowercase letter, a number, and a special character.")]
        public string Password { get; set; }

        [Required]
        public int RoleId { get; set; }

        [ForeignKey("RoleId")]
        public SystemRole? Role { get; set; }
    }
}

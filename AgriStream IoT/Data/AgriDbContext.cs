using Microsoft.EntityFrameworkCore;
using AgriStream_IoT.Models;

namespace AgriStream_IoT.Data
{
    public class AgriDbContext : DbContext
    {
        public AgriDbContext(DbContextOptions<AgriDbContext> options) : base(options) { }

        public DbSet<UserAccount> UserAccounts { get; set; }
        public DbSet<SystemRole> SystemRoles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Explicitly force table naming convention to match controller queries
            modelBuilder.Entity<SystemRole>().ToTable("SystemRoles");
            modelBuilder.Entity<UserAccount>().ToTable("UserAccounts");

            modelBuilder.Entity<UserAccount>()
                .Property(user => user.UserId)
                .ValueGeneratedOnAdd();

            // Seed default roles required by the exam criteria
            modelBuilder.Entity<SystemRole>().HasData(
                new SystemRole { RoleId = 1, RoleName = "Farmer" },
                new SystemRole { RoleId = 2, RoleName = "Agronomist" },
                new SystemRole { RoleId = 3, RoleName = "Admin" }
            );
        }
    }
}

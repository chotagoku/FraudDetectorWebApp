using Microsoft.EntityFrameworkCore;
using FraudDetectorWebApp.Models;

namespace FraudDetectorWebApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<ApiConfiguration> ApiConfigurations { get; set; }
        public DbSet<ApiRequestLog> ApiRequestLogs { get; set; }
        public DbSet<GeneratedScenario> GeneratedScenarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Company).HasMaxLength(100);
                entity.Property(e => e.Role).HasMaxLength(50).HasDefaultValue("User");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.CreatedAt);
                
                // User can have many API configurations
                entity.HasMany(e => e.ApiConfigurations)
                      .WithOne(c => c.User)
                      .HasForeignKey(c => c.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure ApiConfiguration
            modelBuilder.Entity<ApiConfiguration>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ApiEndpoint).IsRequired().HasMaxLength(500);
                entity.Property(e => e.RequestTemplate).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.UserId);
                
                // ApiConfiguration can have many generated scenarios
                entity.HasMany(e => e.GeneratedScenarios)
                      .WithOne(s => s.Configuration)
                      .HasForeignKey(s => s.ConfigurationId)
                      .OnDelete(DeleteBehavior.SetNull);
                
                // Soft deletion query filter
                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configure ApiRequestLog
            modelBuilder.Entity<ApiRequestLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RequestPayload).IsRequired();
                entity.Property(e => e.RequestTimestamp).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(e => e.ApiConfiguration)
                      .WithMany(e => e.RequestLogs)
                      .HasForeignKey(e => e.ApiConfigurationId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                // ApiRequestLog can be linked to a GeneratedScenario
                entity.HasOne(e => e.GeneratedScenario)
                      .WithMany(s => s.RequestLogs)
                      .HasForeignKey(e => e.GeneratedScenarioId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.RequestTimestamp);
                entity.HasIndex(e => new { e.ApiConfigurationId, e.IterationNumber });
                
                // Soft deletion query filter
                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configure GeneratedScenario
            modelBuilder.Entity<GeneratedScenario>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ScenarioJson).IsRequired();
                entity.Property(e => e.RiskLevel).HasMaxLength(20);
                entity.Property(e => e.UserProfile).HasMaxLength(500);
                entity.Property(e => e.UserActivity).HasMaxLength(500);
                entity.Property(e => e.FromName).HasMaxLength(100);
                entity.Property(e => e.ToName).HasMaxLength(100);
                entity.Property(e => e.ActivityCode).HasMaxLength(50);
                entity.Property(e => e.UserType).HasMaxLength(20);
                entity.Property(e => e.ApiEndpoint).HasMaxLength(500);
                entity.Property(e => e.GeneratedAt).HasDefaultValueSql("GETUTCDATE()");
                
                // Decimal precision configuration
                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.Property(e => e.AmountZScore).HasPrecision(10, 2);
                
                // Enhanced fields
                entity.Property(e => e.GeneratedPrompt).HasColumnType("ntext");
                entity.Property(e => e.FromAccount).HasMaxLength(50);
                entity.Property(e => e.ToAccount).HasMaxLength(50);
                entity.Property(e => e.TransactionId).HasMaxLength(50);
                entity.Property(e => e.CNIC).HasMaxLength(20);
                entity.Property(e => e.UserId).HasMaxLength(50);
                entity.Property(e => e.TransactionComments).HasMaxLength(500);
                entity.Property(e => e.ToBank).HasMaxLength(20);
                entity.Property(e => e.LastTestConfiguration).HasMaxLength(200);
                entity.Property(e => e.Tags).HasMaxLength(1000);
                entity.Property(e => e.Notes).HasMaxLength(2000);
                
                // Foreign key relationship with ApiConfiguration (optional)
                entity.HasOne(e => e.Configuration)
                      .WithMany(c => c.GeneratedScenarios)
                      .HasForeignKey(e => e.ConfigurationId)
                      .OnDelete(DeleteBehavior.SetNull);
                
                // Indexes for better performance
                entity.HasIndex(e => e.GeneratedAt);
                entity.HasIndex(e => e.RiskLevel);
                entity.HasIndex(e => e.IsTested);
                entity.HasIndex(e => e.AmountRiskScore);
                entity.HasIndex(e => e.Amount);
                entity.HasIndex(e => e.TestSuccessful);
                entity.HasIndex(e => e.IsFavorite);
                entity.HasIndex(e => e.ConfigurationId);
                entity.HasIndex(e => new { e.RiskLevel, e.IsTested });
                entity.HasIndex(e => new { e.AmountRiskScore, e.Amount });
                entity.HasIndex(e => new { e.ConfigurationId, e.GeneratedAt });
                
                // Soft deletion query filter
                entity.HasQueryFilter(e => !e.IsDeleted);
            });
        }
    }
}

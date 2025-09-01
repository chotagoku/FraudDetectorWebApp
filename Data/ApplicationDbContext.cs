using FraudDetectorWebApp.Models;
using Microsoft.EntityFrameworkCore;

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
        public DbSet<BetaScenario> BetaScenarios { get; set; }
        public DbSet<SystemConfiguration> SystemConfigurations { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        public DbSet<AdminActionLog> AdminActionLogs { get; set; }

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

                // ApiRequestLog can be linked to a BetaScenario
                entity.HasOne(e => e.BetaScenario)
                      .WithMany(s => s.RequestLogs)
                      .HasForeignKey(e => e.BetaScenarioId)
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

            // Configure BetaScenario
            modelBuilder.Entity<BetaScenario>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.UserStory).IsRequired();
                entity.Property(e => e.GeneratedStory).IsRequired();
                entity.Property(e => e.TransactionStory).IsRequired();
                entity.Property(e => e.ScenarioJson).IsRequired();
                entity.Property(e => e.RiskLevel).HasMaxLength(20);
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.UserProfile).HasMaxLength(500);
                entity.Property(e => e.BusinessType).HasMaxLength(100);
                entity.Property(e => e.CustomerSegment).HasMaxLength(100);
                entity.Property(e => e.Currency).HasMaxLength(10);
                entity.Property(e => e.ActivityCode).HasMaxLength(50);
                entity.Property(e => e.UserType).HasMaxLength(20);
                entity.Property(e => e.GeneratedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.GeneratedBy).HasMaxLength(100);
                entity.Property(e => e.Status).HasMaxLength(20);

                // Decimal precision configuration
                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.Property(e => e.AmountZScore).HasPrecision(10, 2);

                // Enhanced fields
                entity.Property(e => e.GenerationPrompt).HasColumnType("ntext");
                entity.Property(e => e.GenerationEngine).HasMaxLength(50);
                entity.Property(e => e.FromName).HasMaxLength(100);
                entity.Property(e => e.ToName).HasMaxLength(100);
                entity.Property(e => e.FromAccount).HasMaxLength(50);
                entity.Property(e => e.ToAccount).HasMaxLength(50);
                entity.Property(e => e.CNIC).HasMaxLength(20);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.IPAddress).HasMaxLength(45);
                entity.Property(e => e.DeviceId).HasMaxLength(50);
                entity.Property(e => e.Location).HasMaxLength(100);
                entity.Property(e => e.TransactionId).HasMaxLength(50);
                entity.Property(e => e.UserId).HasMaxLength(50);
                entity.Property(e => e.TransactionComments).HasMaxLength(500);
                entity.Property(e => e.ToBank).HasMaxLength(20);
                entity.Property(e => e.ApiEndpoint).HasMaxLength(500);
                entity.Property(e => e.Tags).HasMaxLength(1000);
                entity.Property(e => e.Notes).HasMaxLength(2000);
                entity.Property(e => e.SourceDataSummary).HasMaxLength(1000);

                // Foreign key relationships
                entity.HasOne(e => e.Configuration)
                      .WithMany()
                      .HasForeignKey(e => e.ConfigurationId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Self-referencing for scenario derivation - Use NoAction to avoid cascade conflicts
                entity.HasOne(e => e.BaseScenario)
                      .WithMany(e => e.DerivedScenarios)
                      .HasForeignKey(e => e.BasedOnScenarioId)
                      .OnDelete(DeleteBehavior.NoAction);

                // Indexes for better performance
                entity.HasIndex(e => e.GeneratedAt);
                entity.HasIndex(e => e.RiskLevel);
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.IsTested);
                entity.HasIndex(e => e.IsFavorite);
                entity.HasIndex(e => e.Priority);
                entity.HasIndex(e => e.FraudScore);
                entity.HasIndex(e => e.ComplianceScore);
                entity.HasIndex(e => e.Amount);
                entity.HasIndex(e => e.TestSuccessful);
                entity.HasIndex(e => e.GeneratedBy);
                entity.HasIndex(e => e.ConfigurationId);
                entity.HasIndex(e => new { e.RiskLevel, e.IsTested });
                entity.HasIndex(e => new { e.Category, e.Status });
                entity.HasIndex(e => new { e.GeneratedBy, e.GeneratedAt });
                entity.HasIndex(e => new { e.ConfigurationId, e.GeneratedAt });

                // Soft deletion query filter
                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configure SystemConfiguration
            modelBuilder.Entity<SystemConfiguration>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Key).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Value).IsRequired();
                entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DataType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DisplayName).HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Section).HasMaxLength(100);
                entity.Property(e => e.UpdatedBy).HasMaxLength(100);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Unique constraint on Key
                entity.HasIndex(e => e.Key).IsUnique();

                // Indexes for better performance
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.Section);
                entity.HasIndex(e => e.IsReadOnly);
                entity.HasIndex(e => e.RequiresRestart);
                entity.HasIndex(e => e.IsAdvanced);
                entity.HasIndex(e => e.UpdatedAt);
                entity.HasIndex(e => new { e.Category, e.Section });
                entity.HasIndex(e => new { e.IsReadOnly, e.RequiresRestart });

                // Soft deletion query filter
                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configure Role
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.IsSystemRole);
            });

            // Configure Permission
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ResourcePath).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.ResourcePath);
                entity.HasIndex(e => e.IsSystemPermission);
            });

            // Configure UserRole
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.AssignedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.User)
                      .WithMany(u => u.UserRoles)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Role)
                      .WithMany(r => r.UserRoles)
                      .HasForeignKey(e => e.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.AssignedBy)
                      .WithMany()
                      .HasForeignKey(e => e.AssignedByUserId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
                entity.HasIndex(e => e.AssignedAt);
            });

            // Configure RolePermission
            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.AssignedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Role)
                      .WithMany(r => r.RolePermissions)
                      .HasForeignKey(e => e.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Permission)
                      .WithMany(p => p.RolePermissions)
                      .HasForeignKey(e => e.PermissionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.AssignedBy)
                      .WithMany()
                      .HasForeignKey(e => e.AssignedByUserId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
                entity.HasIndex(e => e.AssignedAt);
            });

            // Configure UserPermission
            modelBuilder.Entity<UserPermission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.AssignedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.User)
                      .WithMany(u => u.UserPermissions)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Permission)
                      .WithMany(p => p.UserPermissions)
                      .HasForeignKey(e => e.PermissionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.AssignedBy)
                      .WithMany()
                      .HasForeignKey(e => e.AssignedByUserId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(e => new { e.UserId, e.PermissionId }).IsUnique();
                entity.HasIndex(e => e.AssignedAt);
                entity.HasIndex(e => e.IsGranted);
            });

            // Configure AdminActionLog
            modelBuilder.Entity<AdminActionLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
                entity.Property(e => e.TargetType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.TargetName).HasMaxLength(255);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.IpAddress).HasMaxLength(50);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.ErrorMessage).HasMaxLength(500);
                entity.Property(e => e.ActionAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.AdminUser)
                      .WithMany(u => u.AdminActionLogs)
                      .HasForeignKey(e => e.AdminUserId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(e => e.ActionAt);
                entity.HasIndex(e => e.Action);
                entity.HasIndex(e => e.TargetType);
                entity.HasIndex(e => e.IsSuccessful);
                entity.HasIndex(e => new { e.AdminUserId, e.ActionAt });
            });
        }
    }
}

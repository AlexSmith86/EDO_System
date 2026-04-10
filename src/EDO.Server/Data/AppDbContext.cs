using EDO.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EDO.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        FixDateTimeKinds();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        FixDateTimeKinds();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// PostgreSQL (Npgsql) требует DateTimeKind.Utc для timestamp with time zone.
    /// Конвертируем все Unspecified/Local DateTime в UTC перед записью.
    /// </summary>
    private void FixDateTimeKinds()
    {
        foreach (var entry in ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
        {
            foreach (var prop in entry.Properties)
            {
                if (prop.CurrentValue is DateTime dt && dt.Kind != DateTimeKind.Utc)
                {
                    prop.CurrentValue = dt.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                        : dt.ToUniversalTime();
                }
            }
        }
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Tmc> Tmcs => Set<Tmc>();
    public DbSet<Contractor> Contractors => Set<Contractor>();
    public DbSet<DocumentTemplate> DocumentTemplates => Set<DocumentTemplate>();
    public DbSet<ApprovalStage> ApprovalStages => Set<ApprovalStage>();
    public DbSet<ActionHistory> ActionHistories => Set<ActionHistory>();
    public DbSet<TmcRequest> TmcRequests => Set<TmcRequest>();
    public DbSet<TmcRequestItem> TmcRequestItems => Set<TmcRequestItem>();
    public DbSet<TmcGroup> TmcGroups => Set<TmcGroup>();
    public DbSet<TmcSubgroup> TmcSubgroups => Set<TmcSubgroup>();
    public DbSet<WorkflowChain> WorkflowChains => Set<WorkflowChain>();
    public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Глобально: все DateTime → UTC при записи, SpecifyKind(Utc) при чтении
        configurationBuilder.Properties<DateTime>()
            .HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>()
            .HaveConversion<UtcNullableDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Name).HasMaxLength(100).IsRequired();
            entity.Property(r => r.Description).HasMaxLength(500);
            entity.HasIndex(r => r.Name).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.MiddleName).HasMaxLength(100);
            entity.Property(u => u.Position).HasMaxLength(200).IsRequired();
            entity.Property(u => u.Phone).HasMaxLength(30);
            entity.Property(u => u.TelegramId).HasMaxLength(100);
            entity.Property(u => u.Email).HasMaxLength(200).IsRequired();
            entity.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();

            entity.HasOne(u => u.Role)
                  .WithMany(r => r.Users)
                  .HasForeignKey(u => u.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Tmc>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).HasMaxLength(300).IsRequired();
            entity.Property(t => t.Article).HasMaxLength(100);
            entity.Property(t => t.ExternalId).HasMaxLength(100);
            entity.Property(t => t.StockBalance).HasPrecision(18, 4);
            entity.HasIndex(t => t.ExternalId).IsUnique().HasFilter("\"ExternalId\" IS NOT NULL");
        });

        modelBuilder.Entity<Contractor>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).HasMaxLength(300).IsRequired();
            entity.Property(c => c.Inn).HasMaxLength(12);
            entity.Property(c => c.ExternalId).HasMaxLength(100);
            entity.Property(c => c.ContractorType).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(c => c.ExternalId).IsUnique().HasFilter("\"ExternalId\" IS NOT NULL");
        });

        modelBuilder.Entity<DocumentTemplate>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Name).HasMaxLength(200).IsRequired();
            entity.Property(d => d.FilePath).HasMaxLength(500).IsRequired();
            entity.Property(d => d.ProcessType).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<ApprovalStage>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).HasMaxLength(200).IsRequired();
            entity.Property(s => s.RequiredPosition).HasMaxLength(100).IsRequired().HasDefaultValue("");
            entity.HasOne(s => s.Role)
                  .WithMany()
                  .HasForeignKey(s => s.RoleId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ActionHistory>(entity =>
        {
            entity.HasKey(h => h.Id);
            entity.Property(h => h.Decision).HasConversion<string>().HasMaxLength(20);
            entity.Property(h => h.Comment).HasMaxLength(1000);
            entity.HasOne(h => h.User)
                  .WithMany()
                  .HasForeignKey(h => h.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(h => h.Stage)
                  .WithMany()
                  .HasForeignKey(h => h.StageId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(h => h.WorkflowStep)
                  .WithMany()
                  .HasForeignKey(h => h.WorkflowStepId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(h => h.DocumentId);
        });

        modelBuilder.Entity<TmcGroup>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Code).HasMaxLength(20).IsRequired();
            entity.Property(g => g.Name).HasMaxLength(300).IsRequired();
            entity.HasIndex(g => g.Code).IsUnique();
        });

        modelBuilder.Entity<TmcSubgroup>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Code).HasMaxLength(30).IsRequired();
            entity.Property(s => s.Name).HasMaxLength(500).IsRequired();
            entity.HasOne(s => s.Group)
                  .WithMany(g => g.Subgroups)
                  .HasForeignKey(s => s.GroupId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorkflowChain>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).HasMaxLength(300).IsRequired();
        });

        modelBuilder.Entity<WorkflowStep>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.StepName).HasMaxLength(300).IsRequired();
            entity.Property(s => s.TargetPosition).HasMaxLength(200).IsRequired();
            entity.HasOne(s => s.WorkflowChain)
                  .WithMany(c => c.Steps)
                  .HasForeignKey(s => s.WorkflowChainId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TmcRequest>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(r => r.ProjectName).HasMaxLength(300);
            entity.HasOne(r => r.InitiatorUser)
                  .WithMany()
                  .HasForeignKey(r => r.InitiatorUserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.CurrentStage)
                  .WithMany()
                  .HasForeignKey(r => r.CurrentStageId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.WorkflowChain)
                  .WithMany()
                  .HasForeignKey(r => r.WorkflowChainId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.CurrentWorkflowStep)
                  .WithMany()
                  .HasForeignKey(r => r.CurrentWorkflowStepId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.ResponsibleUser)
                  .WithMany()
                  .HasForeignKey(r => r.ResponsibleUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TmcRequestItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Name).HasMaxLength(500).IsRequired();
            entity.Property(i => i.Quantity).HasPrecision(18, 4);
            entity.Property(i => i.Price).HasPrecision(18, 2);
            entity.Property(i => i.Unit).HasMaxLength(50);
            entity.Property(i => i.InvoiceLink).HasMaxLength(1000);
            entity.Property(i => i.Comment).HasMaxLength(1000);
            entity.Property(i => i.InitiatorName).HasMaxLength(300);
            entity.Property(i => i.InitiatorPosition).HasMaxLength(300);
            entity.HasOne(i => i.TmcRequest)
                  .WithMany(r => r.Items)
                  .HasForeignKey(i => i.TmcRequestId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(i => i.Group)
                  .WithMany()
                  .HasForeignKey(i => i.GroupId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(i => i.Subgroup)
                  .WithMany()
                  .HasForeignKey(i => i.SubgroupId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

/// <summary>Конвертер DateTime → UTC для PostgreSQL (timestamp with time zone)</summary>
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    { }
}

/// <summary>Конвертер DateTime? → UTC для PostgreSQL</summary>
public class UtcNullableDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public UtcNullableDateTimeConverter()
        : base(
            v => v.HasValue
                ? (v.Value.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc))
                : v,
            v => v.HasValue
                ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
                : v)
    { }
}

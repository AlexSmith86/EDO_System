using EDO.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace EDO.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Tmc> Tmcs => Set<Tmc>();
    public DbSet<Contractor> Contractors => Set<Contractor>();
    public DbSet<DocumentTemplate> DocumentTemplates => Set<DocumentTemplate>();

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
            entity.HasIndex(t => t.ExternalId).IsUnique().HasFilter("[ExternalId] IS NOT NULL");
        });

        modelBuilder.Entity<Contractor>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).HasMaxLength(300).IsRequired();
            entity.Property(c => c.Inn).HasMaxLength(12);
            entity.Property(c => c.ExternalId).HasMaxLength(100);
            entity.Property(c => c.ContractorType).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(c => c.ExternalId).IsUnique().HasFilter("[ExternalId] IS NOT NULL");
        });

        modelBuilder.Entity<DocumentTemplate>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Name).HasMaxLength(200).IsRequired();
            entity.Property(d => d.FilePath).HasMaxLength(500).IsRequired();
            entity.Property(d => d.ProcessType).HasMaxLength(100).IsRequired();
        });
    }
}

using GMVM.EnergyTracker.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace GMVM.EnergyTracker.Infrastructure;

/// <summary>
/// EF Core DbContext para EnergyTracker.
/// Usa SQLite para zero-setup en el demo.
/// </summary>
public class EnergyTrackerDbContext : DbContext
{
    public EnergyTrackerDbContext(DbContextOptions<EnergyTrackerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Medidor> Medidores => Set<Medidor>();
    public DbSet<Lectura> Lecturas => Set<Lectura>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Medidor>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Serial).IsRequired().HasMaxLength(50);
            entity.Property(m => m.Ubicacion).IsRequired().HasMaxLength(200);
            entity.Property(m => m.ClienteId).IsRequired().HasMaxLength(50);
            entity.HasIndex(m => m.Serial).IsUnique();
            entity.HasIndex(m => m.ClienteId);
        });

        modelBuilder.Entity<Lectura>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.ValorKwh).HasPrecision(18, 4);
            entity.Property(l => l.ConsumoCalculado).HasPrecision(18, 4);
            entity.HasOne(l => l.Medidor)
                  .WithMany(m => m.Lecturas)
                  .HasForeignKey(l => l.MedidorId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(l => new { l.MedidorId, l.FechaLectura });
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(u => u.Rol).IsRequired().HasMaxLength(20);
            entity.HasIndex(u => u.Email).IsUnique();
        });
    }
}

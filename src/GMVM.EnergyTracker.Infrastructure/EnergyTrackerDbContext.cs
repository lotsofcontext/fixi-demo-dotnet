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
    }
}

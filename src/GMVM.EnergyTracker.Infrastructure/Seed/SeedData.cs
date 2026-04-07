using GMVM.EnergyTracker.Domain.Models;

namespace GMVM.EnergyTracker.Infrastructure.Seed;

/// <summary>
/// Genera datos sembrados deterministicos para el demo:
/// 50 medidores y ~5000 lecturas distribuidas en el tiempo.
/// </summary>
public static class SeedData
{
    private const int MedidoresCount = 50;
    private const int LecturasPorMedidor = 100;
    private static readonly string[] Clientes = { "ISAGEN", "EPM", "XM", "VEOLIA", "CELSIA" };
    private static readonly string[] Ciudades =
    {
        "Medellin", "Bogota", "Cali", "Barranquilla", "Cartagena",
        "Bucaramanga", "Pereira", "Manizales", "Santa Marta", "Cucuta"
    };

    public static void Seed(EnergyTrackerDbContext db)
    {
        if (db.Medidores.Any())
        {
            return;
        }

        // Random deterministico para que la demo sea reproducible.
        var random = new Random(20260407);

        var medidores = new List<Medidor>();
        for (var i = 1; i <= MedidoresCount; i++)
        {
            var cliente = Clientes[i % Clientes.Length];
            var ciudad = Ciudades[i % Ciudades.Length];
            medidores.Add(new Medidor
            {
                Serial = $"MED-{cliente}-{i:D5}",
                Ubicacion = $"Cra {random.Next(1, 100)} #{random.Next(1, 100)}-{random.Next(1, 99)}, {ciudad}",
                ClienteId = cliente,
                FechaInstalacion = new DateTime(2024, 1, 1).AddDays(random.Next(0, 730))
            });
        }
        db.Medidores.AddRange(medidores);
        db.SaveChanges();

        // Lecturas: ~100 por medidor, una cada ~3 dias, valor monotonico creciente.
        var lecturas = new List<Lectura>();
        var fechaBase = new DateTime(2025, 1, 1);
        foreach (var medidor in medidores)
        {
            decimal valorAcumulado = random.Next(1000, 10000);
            for (var l = 0; l < LecturasPorMedidor; l++)
            {
                var fecha = fechaBase.AddDays(l * 3 + random.Next(0, 2));
                valorAcumulado += random.Next(50, 500);
                lecturas.Add(new Lectura
                {
                    MedidorId = medidor.Id,
                    FechaLectura = fecha,
                    ValorKwh = valorAcumulado,
                    ConsumoCalculado = 0m // Sera recalculado por el servicio en runtime.
                });
            }
        }
        db.Lecturas.AddRange(lecturas);
        db.SaveChanges();
    }
}

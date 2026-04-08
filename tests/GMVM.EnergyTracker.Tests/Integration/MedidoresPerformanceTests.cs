using System.Data.Common;
using System.Diagnostics;
using GMVM.EnergyTracker.Domain.Models;
using GMVM.EnergyTracker.Infrastructure;
using GMVM.EnergyTracker.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GMVM.EnergyTracker.Tests.Integration;

/// <summary>
/// Tests de performance y eficiencia de queries para WI-102.
/// Verifica que <see cref="MedidorService.ListarConResumenAsync"/>
/// no presente el patron N+1 y complete en tiempo aceptable.
/// </summary>
public class MedidoresPerformanceTests : IDisposable
{
    private const int MedidoresCount = 100;
    private const int LecturasPorMedidor = 10;

    private readonly SqliteConnection _connection;
    private readonly EnergyTrackerDbContext _db;
    private readonly QueryCountInterceptor _queryCounter;

    public MedidoresPerformanceTests()
    {
        _queryCounter = new QueryCountInterceptor();
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<EnergyTrackerDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(_queryCounter)
            .Options;

        _db = new EnergyTrackerDbContext(options);
        _db.Database.EnsureCreated();

        SeedTestData();
    }

    /// <summary>
    /// WI-102 regression: el metodo debe ejecutar a lo sumo 2 queries SQL,
    /// independientemente del numero de medidores.
    /// </summary>
    [Fact]
    public async Task ListarConResumen_Con100Medidores_EjecutaMaximo2Queries()
    {
        // Arrange
        var service = new MedidorService(_db);
        _queryCounter.Reset();

        // Act
        var resultado = await service.ListarConResumenAsync();

        // Assert
        Assert.Equal(MedidoresCount, resultado.Count);
        Assert.True(
            _queryCounter.Count <= 2,
            $"Se ejecutaron {_queryCounter.Count} queries SQL — se esperaban <=2. " +
            $"Probable N+1 (1 + {MedidoresCount} = {1 + MedidoresCount} queries).");
    }

    /// <summary>
    /// WI-102 regression: con 100 medidores y 10 lecturas cada uno,
    /// el metodo debe completar en menos de 500ms.
    /// </summary>
    [Fact]
    public async Task ListarConResumen_Con100Medidores_CompletaEnMenosDe500ms()
    {
        // Arrange
        var service = new MedidorService(_db);

        // Warmup (EF compile cache, SQLite connection)
        _ = await service.ListarConResumenAsync();

        // Act
        var sw = Stopwatch.StartNew();
        var resultado = await service.ListarConResumenAsync();
        sw.Stop();

        // Assert
        Assert.Equal(MedidoresCount, resultado.Count);
        Assert.True(
            sw.ElapsedMilliseconds < 500,
            $"ListarConResumenAsync tardo {sw.ElapsedMilliseconds}ms — se esperaban <500ms.");
    }

    /// <summary>
    /// Verifica que el contrato de respuesta no cambie: todos los campos
    /// del DTO deben estar presentes y correctamente mapeados.
    /// </summary>
    [Fact]
    public async Task ListarConResumen_CamposDelDtoMapeadosCorrectamente()
    {
        // Arrange
        var service = new MedidorService(_db);

        // Act
        var resultado = await service.ListarConResumenAsync();

        // Assert: todos los medidores tienen datos basicos.
        Assert.All(resultado, item =>
        {
            Assert.True(item.Id > 0);
            Assert.False(string.IsNullOrEmpty(item.Serial));
            Assert.False(string.IsNullOrEmpty(item.Ubicacion));
            Assert.False(string.IsNullOrEmpty(item.ClienteId));
        });

        // Todos tienen lecturas, asi que FechaUltimaLectura y ValorUltimaLectura no deben ser null.
        Assert.All(resultado, item =>
        {
            Assert.NotNull(item.FechaUltimaLectura);
            Assert.NotNull(item.ValorUltimaLectura);
        });
    }

    private void SeedTestData()
    {
        var random = new Random(42);
        var clientes = new[] { "ISAGEN", "EPM", "XM", "VEOLIA", "CELSIA" };

        var medidores = new List<Medidor>();
        for (var i = 1; i <= MedidoresCount; i++)
        {
            var cliente = clientes[i % clientes.Length];
            medidores.Add(new Medidor
            {
                Serial = $"TEST-{i:D5}",
                Ubicacion = $"Test Location {i}",
                ClienteId = cliente,
                FechaInstalacion = new DateTime(2024, 1, 1).AddDays(random.Next(0, 365))
            });
        }
        _db.Medidores.AddRange(medidores);
        _db.SaveChanges();

        var lecturas = new List<Lectura>();
        var fechaBase = new DateTime(2025, 6, 1);
        foreach (var medidor in medidores)
        {
            decimal valorAcumulado = random.Next(1000, 10000);
            for (var l = 0; l < LecturasPorMedidor; l++)
            {
                var fecha = fechaBase.AddDays(l * 3);
                valorAcumulado += random.Next(50, 500);
                lecturas.Add(new Lectura
                {
                    MedidorId = medidor.Id,
                    FechaLectura = fecha,
                    ValorKwh = valorAcumulado,
                    ConsumoCalculado = 0m
                });
            }
        }
        _db.Lecturas.AddRange(lecturas);
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    /// <summary>
    /// Interceptor que cuenta las queries SQL ejecutadas por EF Core.
    /// </summary>
    private class QueryCountInterceptor : DbCommandInterceptor
    {
        private int _count;

        public int Count => _count;

        public void Reset() => _count = 0;

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            Interlocked.Increment(ref _count);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _count);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}

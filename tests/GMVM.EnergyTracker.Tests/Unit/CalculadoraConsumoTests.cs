using GMVM.EnergyTracker.Domain.Models;
using GMVM.EnergyTracker.Domain.Services;

namespace GMVM.EnergyTracker.Tests.Unit;

/// <summary>
/// Tests para <see cref="CalculadoraConsumo"/>.
/// El test <c>Calcular_DosLecturasMismoDia_NoDebeLanzarExcepcion</c>
/// captura el bug WI-101 — debe FALLAR antes del fix de Fixi y PASAR despues.
/// </summary>
public class CalculadoraConsumoTests
{
    private readonly CalculadoraConsumo _sut = new();

    [Fact]
    public void Calcular_PrimeraLecturaSinPrevia_RetornaValorActual()
    {
        var actual = new Lectura
        {
            FechaLectura = new DateTime(2026, 4, 1),
            ValorKwh = 1500m
        };

        var resultado = _sut.Calcular(previa: null, actual);

        Assert.Equal(1500m, resultado);
    }

    [Fact]
    public void Calcular_DosLecturasEnDiasDistintos_PromediaPorDia()
    {
        var previa = new Lectura
        {
            FechaLectura = new DateTime(2026, 4, 1, 8, 0, 0),
            ValorKwh = 1000m
        };
        var actual = new Lectura
        {
            FechaLectura = new DateTime(2026, 4, 11, 8, 0, 0), // 10 dias despues
            ValorKwh = 1500m
        };

        var resultado = _sut.Calcular(previa, actual);

        Assert.Equal(50m, resultado); // (1500 - 1000) / 10 = 50 kWh/dia
    }

    /// <summary>
    /// REGRESION DE WI-101: dos lecturas el mismo dia NO deben lanzar excepcion.
    /// Caso real: tecnico de campo registra dos lecturas el mismo dia (revision post-tormenta).
    /// </summary>
    [Fact]
    public void Calcular_DosLecturasMismoDia_NoDebeLanzarExcepcion()
    {
        var previa = new Lectura
        {
            FechaLectura = new DateTime(2026, 4, 6, 8, 0, 0),
            ValorKwh = 1000m
        };
        var actual = new Lectura
        {
            FechaLectura = new DateTime(2026, 4, 6, 14, 0, 0), // mismo dia, diferente hora
            ValorKwh = 1050m
        };

        var ex = Record.Exception(() => _sut.Calcular(previa, actual));

        Assert.Null(ex); // Bug WI-101: actualmente lanza DivideByZeroException
    }

    /// <summary>
    /// Para reforzar la solucion correcta de WI-101: el consumo debe ser el delta directo
    /// cuando las lecturas son sub-diarias (intra-day).
    /// </summary>
    [Fact]
    public void Calcular_DosLecturasMismoDia_RetornaDeltaDirecto()
    {
        var previa = new Lectura
        {
            FechaLectura = new DateTime(2026, 4, 6, 8, 0, 0),
            ValorKwh = 1000m
        };
        var actual = new Lectura
        {
            FechaLectura = new DateTime(2026, 4, 6, 14, 0, 0),
            ValorKwh = 1050m
        };

        var resultado = _sut.Calcular(previa, actual);

        Assert.Equal(50m, resultado);
    }
}

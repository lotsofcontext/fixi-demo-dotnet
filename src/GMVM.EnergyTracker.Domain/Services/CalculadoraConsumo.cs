using GMVM.EnergyTracker.Domain.Models;

namespace GMVM.EnergyTracker.Domain.Services;

/// <summary>
/// Implementacion de <see cref="ICalculadoraConsumo"/>.
/// Calcula consumo promediando el delta de kWh sobre los dias transcurridos.
/// </summary>
public class CalculadoraConsumo : ICalculadoraConsumo
{
    public decimal Calcular(Lectura? previa, Lectura actual)
    {
        if (previa is null)
        {
            // Primera lectura del medidor: no hay delta que calcular.
            return actual.ValorKwh;
        }

        var diasTranscurridos = (actual.FechaLectura - previa.FechaLectura).Days;
        var deltaKwh = actual.ValorKwh - previa.ValorKwh;

        // WI-101: cuando las lecturas son del mismo dia (intra-day), diasTranscurridos
        // es 0 (.Days trunca a dias enteros). Retornamos el delta directo sin promediar,
        // ya que el consumo diario promedio no tiene sentido para periodos sub-diarios.
        if (diasTranscurridos == 0)
        {
            return deltaKwh;
        }

        // Promedio diario de consumo entre las dos lecturas.
        return deltaKwh / diasTranscurridos;
    }
}

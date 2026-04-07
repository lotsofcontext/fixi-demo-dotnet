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

        // Lecturas sub-diarias (mismo dia o diferencia < 24h): un mismo medidor puede
        // tener multiples lecturas el mismo dia (ej: revisiones post-tormenta). En ese
        // caso retornamos el delta acumulado directo, sin promediar (evita division por
        // cero y respeta la regla de negocio descrita en WI-101).
        if (diasTranscurridos < 1)
        {
            return deltaKwh;
        }

        // Promedio diario de consumo entre las dos lecturas.
        return deltaKwh / diasTranscurridos;
    }
}

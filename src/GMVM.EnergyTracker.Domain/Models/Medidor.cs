namespace GMVM.EnergyTracker.Domain.Models;

/// <summary>
/// Medidor de energia electrica instalado en sitio del cliente.
/// </summary>
public class Medidor
{
    public int Id { get; set; }

    /// <summary>
    /// Numero de serie unico del medidor (impreso en la placa).
    /// </summary>
    public string Serial { get; set; } = string.Empty;

    /// <summary>
    /// Direccion fisica donde esta instalado el medidor.
    /// </summary>
    public string Ubicacion { get; set; } = string.Empty;

    /// <summary>
    /// Identificador del cliente al que pertenece el medidor.
    /// Ejemplo: ISAGEN, EPM, XM, Veolia.
    /// </summary>
    public string ClienteId { get; set; } = string.Empty;

    public DateTime FechaInstalacion { get; set; }

    /// <summary>
    /// Lecturas historicas del medidor (relacion 1:N).
    /// </summary>
    public List<Lectura> Lecturas { get; set; } = new();
}

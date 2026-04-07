namespace GMVM.EnergyTracker.Domain.Models;

/// <summary>
/// Usuario del sistema con rol de acceso.
/// </summary>
public class Usuario
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Rol del usuario: "User" o "Admin".
    /// Los usuarios con rol Admin pueden ejecutar operaciones destructivas.
    /// </summary>
    public string Rol { get; set; } = "User";

    public DateTime CreadoEn { get; set; }
}

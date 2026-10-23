using Microsoft.AspNetCore.Identity;

namespace PergamonLens.Infrastructure.Identity;

/// <summary>
/// Representa el usuario de la aplicación en el sistema de ASP.NET Core Identity.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Fecha de registro en formato UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Nombre visible opcional del coleccionista.
    /// </summary>
    public string? DisplayName { get; set; }
}

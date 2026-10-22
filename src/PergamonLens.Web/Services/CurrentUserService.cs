using System.Security.Claims;
using PergamonLens.Application.Common.Interfaces;

namespace PergamonLens.Web.Services;

/// <summary>
/// Provee el identificador del usuario autenticado en la petición HTTP actual
/// a través de los Claims de ASP.NET Core Identity.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}

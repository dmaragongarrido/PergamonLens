namespace PergamonLens.Application.Common.Exceptions;

/// <summary>
/// Excepción lanzada cuando una entidad solicitada no se encuentra en el sistema.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string name, object key)
        : base($"La entidad \"{name}\" con clave ({key}) no fue encontrada.")
    {
    }
}


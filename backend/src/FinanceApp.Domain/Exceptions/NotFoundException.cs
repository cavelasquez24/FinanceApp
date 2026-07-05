namespace FinanceApp.Domain.Exceptions;

/// <summary>
/// Se lanza cuando un recurso no existe o no pertenece al usuario.
/// El middleware la captura y retorna un 404 Not Found.
/// 404 incluso cuando el recurso existe pero no pertenece
/// al usuario — evita revelar información a atacantes.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string resource)
        : base($"{resource} no encontrado") { }

    public NotFoundException(string resource, Guid id)
        : base($"{resource} con id '{id}' no encontrado") { }
}
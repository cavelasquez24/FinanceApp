namespace FinanceApp.Domain.Exceptions;

/// <summary>
/// Excepción base para errores de reglas de negocio.
/// Cuando algo viola una regla del dominio lanzamos esta excepción.
/// El middleware la captura y retorna un 400 Bad Request.
/// </summary>
public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}
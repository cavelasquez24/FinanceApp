namespace FinanceApp.Domain.Exceptions;

/// <summary>
/// Se lanza cuando el usuario no está autenticado o
/// sus credenciales son inválidas.
/// El middleware la captura y retorna un 401 Unauthorized.
/// </summary>
public class UnauthorizedException : Exception
{
    public string Code { get; }

    public UnauthorizedException(string code, string message) : base(message)
    {
        Code = code;
    }
}
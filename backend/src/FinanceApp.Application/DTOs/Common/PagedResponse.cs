namespace FinanceApp.Application.DTOs.Common;

/// <summary>
/// Respuesta paginada para listados.
/// Retornar múltiples registros con paginación.
/// </summary>
public class PagedResponse<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }

    /// <summary>
    /// Calculado automáticamente, no se almacena.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
namespace FinanceApp.Application.Interfaces;

/// <summary>Fuente explícita y testeable de la fecha civil de negocio.</summary>
public interface IBusinessDateProvider
{
    DateOnly Today { get; }
    DateOnly GetDate(DateTimeOffset instant);
}

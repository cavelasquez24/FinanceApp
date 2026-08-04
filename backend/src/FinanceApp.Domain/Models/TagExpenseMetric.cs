namespace FinanceApp.Domain.Models;

public sealed record TagExpenseMetric(
    Guid TagId,
    string Name,
    string? Color,
    decimal TotalAmount,
    int ExpenseCount);

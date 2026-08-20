namespace FinanceApp.Application.DTOs.Account;

public record ReconciliationPreviewDto(
    Guid AccountId,
    string AccountName,
    decimal LedgerBalance,
    decimal CurrentBalance,
    DateOnly? LastReconciliationDate,
    decimal? LastReconciliationActualBalance
);

public record ReconciliationCreateDto(
    decimal ActualBalance,
    DateOnly ReconciliationDate,
    string? Notes
);

public record ReconciliationResponseDto(
    Guid Id,
    Guid AccountId,
    string AccountName,
    DateOnly ReconciliationDate,
    decimal ExpectedBalance,
    decimal ActualBalance,
    decimal Difference,
    bool AdjustmentCreated,
    Guid? AdjustmentTransactionId,
    string? Notes,
    string Status,
    DateTimeOffset CreatedAt
);

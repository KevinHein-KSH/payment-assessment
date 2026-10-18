using PaymentAssessment.Domain.Enums;

namespace PaymentAssessment.Application.Transactions;

// Shape matches TransactionDetails in frontend/lib/types.ts (Result page).
public record TransactionDetailsDto(
    string InvoiceNo,
    TransactionStatus Status,
    int CustomerId,
    int ProductId,
    string ProductName,
    decimal Amount,
    string Currency,
    string? PaymentChannel,
    string? MaskedCardNo,
    string? RespCode,
    string? RespDesc);

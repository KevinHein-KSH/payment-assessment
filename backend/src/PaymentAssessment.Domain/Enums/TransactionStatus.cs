namespace PaymentAssessment.Domain.Enums;

/// <summary>Lifecycle state of a payment transaction.</summary>
public enum TransactionStatus
{
    Pending,
    Succeeded,
    Failed
}

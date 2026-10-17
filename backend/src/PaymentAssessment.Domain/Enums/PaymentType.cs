namespace PaymentAssessment.Domain.Enums;

/// <summary>How a product is paid for: a single charge or a recurring charge that saves a card.</summary>
public enum PaymentType
{
    OneTime,
    Recurring
}

using PaymentAssessment.Domain.Enums;

namespace PaymentAssessment.Application.Lookups;

// Shapes match frontend/lib/types.ts so the Checkout page can swap mock data for fetch calls.
public record CustomerDto(int Id, string FirstName, string LastName, string Email);

public record ProductDto(int Id, string Name, string Description, decimal Price, string Currency, PaymentType PaymentType);

// No CustomerToken: it must never reach the browser.
public record CustomerCardDto(int Id, string MaskedCardNo);

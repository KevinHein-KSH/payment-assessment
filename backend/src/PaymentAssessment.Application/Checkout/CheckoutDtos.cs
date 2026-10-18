using PaymentAssessment.Domain.Enums;

namespace PaymentAssessment.Application.Checkout;

// Shapes match frontend/lib/types.ts. The Idempotency-Key travels in the header, not the body.
public record CheckoutRequest(int CustomerId, int ProductId, int? CustomerCardId);

// RedirectUrl: hosted page or 3DS; null when a saved-card payment needs no redirect (docs/02).
public record CheckoutResponse(string InvoiceNo, TransactionStatus Status, string? RedirectUrl);

/// <summary>Tells the controller which status code to return (same pattern as NotificationOutcome).</summary>
public enum CheckoutOutcome { Ok, NotFound, InProgress }

/// <summary>Response is set only when Outcome is Ok (a new checkout or a replay of the same Idempotency-Key).</summary>
public record CheckoutResult(CheckoutOutcome Outcome, CheckoutResponse? Response = null);

using PaymentAssessment.Application.Checkout;
using PaymentAssessment.Application.Lookups;
using PaymentAssessment.Application.Payments;
using PaymentAssessment.Application.Transactions;
using PaymentAssessment.Domain.Enums;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PaymentAssessment.Api.Swagger;

// Swagger request/response examples (docs/02), picked up by type. Values are from real sandbox runs (2026-10-18).

public class CustomersExample : IExamplesProvider<IEnumerable<CustomerDto>>
{
    public IEnumerable<CustomerDto> GetExamples() => [new(3, "Chloe", "Ng", "chloe.ng@example.com")];
}

public class ProductsExample : IExamplesProvider<IEnumerable<ProductDto>>
{
    public IEnumerable<ProductDto> GetExamples() =>
    [
        new(1, "Single Class Pass", "One drop-in class", 25.00m, "SGD", PaymentType.OneTime),
        new(3, "Monthly Membership", "Unlimited classes, billed monthly", 149.00m, "SGD", PaymentType.Recurring)
    ];
}

public class CustomerCardsExample : IExamplesProvider<IEnumerable<CustomerCardDto>>
{
    public IEnumerable<CustomerCardDto> GetExamples() => [new(3, "411111XXXXXX1111")];
}

public class CheckoutRequestExamples : IMultipleExamplesProvider<CheckoutRequest>
{
    public IEnumerable<SwaggerExample<CheckoutRequest>> GetExamples()
    {
        yield return SwaggerExample.Create("New payment method", new CheckoutRequest(3, 3, null));
        yield return SwaggerExample.Create("Saved card", new CheckoutRequest(3, 3, 3));
    }
}

public class CheckoutResponseExamples : IMultipleExamplesProvider<CheckoutResponse>
{
    public IEnumerable<SwaggerExample<CheckoutResponse>> GetExamples()
    {
        yield return SwaggerExample.Create("New payment method", new CheckoutResponse("INV2026101807521038ABCB",
            TransactionStatus.Pending, "https://sandbox-pgw-ui.2c2p.com/payment/4.3/#/token/kSAops9Zwhos8hSTSeLTU..."));
        yield return SwaggerExample.Create("Saved card", new CheckoutResponse("INV20261018073829747AAF",
            TransactionStatus.Succeeded, null));
    }
}

public class NotificationRequestExample : IExamplesProvider<PaymentNotificationRequest>
{
    public PaymentNotificationRequest GetExamples()
        => new("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpbnZvaWNlTm8iOiJJTlYyMDI2...");
}

public class TransactionDetailsExamples : IMultipleExamplesProvider<TransactionDetailsDto>
{
    public IEnumerable<SwaggerExample<TransactionDetailsDto>> GetExamples()
    {
        yield return SwaggerExample.Create("Succeeded", new TransactionDetailsDto("INV20261018073829747AAF",
            TransactionStatus.Succeeded, 3, 3, "Monthly Membership", 149.00m, "SGD", "VI", "411111XXXXXX1111", null, null));
        yield return SwaggerExample.Create("Failed", new TransactionDetailsDto("INV202610180751499B9446",
            TransactionStatus.Failed, 1, 1, "Single Class Pass", 25.00m, "SGD", null, null, "4001", "Refer to cared issuer"));
    }
}

/// <summary>Example value for the Idempotency-Key header on POST /api/checkout.</summary>
public class IdempotencyKeyExampleFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var header = operation.Parameters.FirstOrDefault(p => p.Name == "Idempotency-Key");
        if (header is not null)
            header.Example = new OpenApiString("5f0c7a9e-3b1d-4c2a-9e8f-1a2b3c4d5e6f");
    }
}

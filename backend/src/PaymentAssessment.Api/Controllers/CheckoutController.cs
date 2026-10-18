using Microsoft.AspNetCore.Mvc;
using PaymentAssessment.Application.Checkout;

namespace PaymentAssessment.Api.Controllers;

[ApiController]
[Route("api/checkout")]
public class CheckoutController(CheckoutService checkout) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CheckoutResponse>> Checkout(
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey, // non-nullable → missing header = automatic 400
        CheckoutRequest request, CancellationToken ct)
    {
        var result = await checkout.CheckoutAsync(request, idempotencyKey, ct);
        return result.Outcome switch
        {
            CheckoutOutcome.NotFound => Problem(statusCode: 404, title: "Unknown customer or product"),
            CheckoutOutcome.InProgress => Problem(statusCode: 409, title: "A checkout with this Idempotency-Key is in progress"),
            _ => Ok(result.Response)
        };
    }
}

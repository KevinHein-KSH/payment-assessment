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
        var response = await checkout.CheckoutAsync(request, idempotencyKey, ct);
        if (response is null)
            return Problem(statusCode: 404, title: "Unknown customer or product");
        return Ok(response);
    }
}

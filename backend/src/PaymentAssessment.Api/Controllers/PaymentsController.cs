using Microsoft.AspNetCore.Mvc;
using PaymentAssessment.Application.Payments;

namespace PaymentAssessment.Api.Controllers;

[ApiController]
[Route("api/payments/2c2p")]
public class PaymentsController(NotificationService notifications) : ControllerBase
{
    /// <summary>The 2C2P backendReturnUrl (docs/05, Backend notification).</summary>
    [HttpPost("notification")]
    public async Task<IActionResult> Notification(PaymentNotificationRequest request, CancellationToken ct)
    {
        var outcome = await notifications.HandleAsync(request.Payload, ct);
        return outcome switch
        {
            NotificationOutcome.InvalidSignature => Problem(statusCode: 400, title: "Invalid signature"),
            NotificationOutcome.Mismatch => Problem(statusCode: 400, title: "Mismatch"),
            NotificationOutcome.NotFound => Problem(statusCode: 404, title: "Not found"),
            NotificationOutcome.Processed => Ok(),
            _ => throw new InvalidOperationException($"Unexpected outcome: {outcome}")
        };
    }
}

using Microsoft.AspNetCore.Mvc;
using PaymentAssessment.Application.Payments;
using PaymentAssessment.Application.Transactions;

namespace PaymentAssessment.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController(ITransactionRepository transactions, NotificationService notifications) : ControllerBase
{
    /// <summary>Result page read. No cache: status is always read fresh (docs/05). A Pending row is first checked with Payment Inquiry.</summary>
    [HttpGet("{invoiceNo}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionDetailsDto>> Get(string invoiceNo, CancellationToken ct)
    {
        await notifications.RefreshPendingAsync(invoiceNo, ct);
        var details = await transactions.GetDetailsAsync(invoiceNo, ct);
        if (details is null)
            return NotFound();
        return Ok(details);
    }
}

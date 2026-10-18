using Microsoft.AspNetCore.Mvc;
using PaymentAssessment.Application.Transactions;

namespace PaymentAssessment.Api.Controllers;

// Calls the repository directly: a service with one pass-through method would add nothing.
[ApiController]
[Route("api/transactions")]
public class TransactionsController(ITransactionRepository transactions) : ControllerBase
{
    /// <summary>Result page read. No cache: status is always read fresh (docs/05).</summary>
    [HttpGet("{invoiceNo}")]
    public async Task<ActionResult<TransactionDetailsDto>> Get(string invoiceNo, CancellationToken ct)
    {
        var details = await transactions.GetDetailsAsync(invoiceNo, ct);
        if (details is null)
            return NotFound();
        return Ok(details);
    }
}

using Microsoft.AspNetCore.Mvc;
using PaymentAssessment.Application.Lookups;

namespace PaymentAssessment.Api.Controllers;

[ApiController]
[Route("api")]
public class LookupsController(LookupService lookups) : ControllerBase
{
    [HttpGet("customers")]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetCustomers(CancellationToken ct)
        => Ok(await lookups.GetCustomersAsync(ct));

    [HttpGet("products")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts(CancellationToken ct)
        => Ok(await lookups.GetProductsAsync(ct));

    [HttpGet("customers/{id:int}/cards")]
    public async Task<ActionResult<IEnumerable<CustomerCardDto>>> GetCustomerCards(int id, CancellationToken ct)
    {
        var cards = await lookups.GetCustomerCardsAsync(id, ct);
        return cards == null ? NotFound() : Ok(cards);
    }
}

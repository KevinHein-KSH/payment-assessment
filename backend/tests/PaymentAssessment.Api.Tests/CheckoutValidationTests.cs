using System.Net;
using System.Net.Http.Json;

namespace PaymentAssessment.Api.Tests;

/// <summary>Checkout rejections from docs/02 (Errors and validation). None of these reach 2C2P.</summary>
[Collection("Api")]
public class CheckoutValidationTests(ApiFactory factory)
{
    private const string Url = "/api/checkout";
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Checkout_without_idempotency_key_returns_400()
    {
        var res = await _client.PostAsJsonAsync(Url, new { customerId = 1, productId = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Theory]
    [InlineData(999, 1)] // unknown customer
    [InlineData(1, 999)] // unknown product
    public async Task Checkout_with_unknown_customer_or_product_returns_404_problem(int customerId, int productId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Url)
        {
            Content = JsonContent.Create(new { customerId, productId })
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var res = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        // A missing route is also 404, so require Problem Details to prove the endpoint answered
        Assert.Equal("application/problem+json", res.Content.Headers.ContentType?.MediaType);
    }
}

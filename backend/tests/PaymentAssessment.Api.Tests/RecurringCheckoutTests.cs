using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PaymentAssessment.Application.Checkout;

namespace PaymentAssessment.Api.Tests;

/// <summary>docs/05 Payment Token parameters: Recurring products ask 2C2P to tokenize the card, OneTime ones don't.</summary>
[Collection("FakeProviderApi")]
public class RecurringCheckoutTests(FakeProviderApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private readonly HttpClient _client = factory.CreateClient();

    // Seeds (AppDbContext): product 1 is OneTime, product 3 is Recurring
    [Theory]
    [InlineData(1, false)]
    [InlineData(3, true)]
    public async Task Checkout_asks_to_save_the_card_only_for_recurring_products(int productId, bool expectedSaveCard)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/checkout")
        {
            Content = JsonContent.Create(new { customerId = 1, productId })
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var res = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = (await res.Content.ReadFromJsonAsync<CheckoutResponse>(Json))!;
        Assert.Equal(expectedSaveCard, factory.Provider.SaveCardRequests[body.InvoiceNo]);
    }
}

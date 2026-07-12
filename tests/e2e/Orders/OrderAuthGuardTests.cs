namespace Tekram.E2E.Orders;

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

/// <summary>
/// Black-box negative-path coverage for issue #63 (auth-guard half): a protected
/// endpoint hit with no bearer token must return 401, never 404 (unmapped) or
/// 500 (binding failure).
/// </summary>
[Trait("issue", "63")]
public class OrderAuthGuardTests : LiveApiTestBase
{
    [LiveFact]
    public async Task AC2_OrderWithoutToken_Returns401()
    {
        // Well-formed payload, no Authorization header.
        var resp = await Client.PostAsJsonAsync("/api/food/orders", new
        {
            restaurantId = Guid.NewGuid(),
            items = new[]
            {
                new { menuItemId = Guid.NewGuid(), quantity = 1, customizationChoices = (object[]?)null },
            },
            deliveryAddress = "Test Street, Beirut",
            paymentMethod = "COD",
            couponCode = (string?)null,
            specialInstructions = (string?)null,
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "protected endpoints must reject unauthenticated requests with 401");
    }
}

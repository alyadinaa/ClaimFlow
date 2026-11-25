using ClaimFlow.Api.Modules.Claims;
using ClaimFlow.Api.Modules.Claims.Entities;

namespace ClaimFlow.Tests;

public class MarketCatalogTests
{
    [Theory]
    [InlineData(Market.Malaysia, "MYR", 2, "Asia/Kuala_Lumpur")]
    [InlineData(Market.Singapore, "SGD", 2, "Asia/Singapore")]
    [InlineData(Market.Australia, "AUD", 2, "Australia/Sydney")]
    [InlineData(Market.HongKong, "HKD", 2, "Asia/Hong_Kong")]
    [InlineData(Market.Japan, "JPY", 0, "Asia/Tokyo")]
    [InlineData(Market.NewZealand, "NZD", 2, "Pacific/Auckland")]
    public void Market_has_approved_settings(Market market, string currency, int decimals, string timeZone)
    {
        Assert.Equal(new MarketInfo(currency, decimals, timeZone), MarketCatalog.Get(market));
        // Confirms that the time zone exists on this machine.
        MarketCatalog.LocalDate(market, DateTimeOffset.UnixEpoch);
    }

    [Fact]
    public void Local_date_can_differ_between_markets()
    {
        var instant = new DateTimeOffset(2026, 11, 25, 12, 0, 0, TimeSpan.Zero);

        Assert.Equal(new DateOnly(2026, 11, 25), MarketCatalog.LocalDate(Market.Singapore, instant));
        Assert.Equal(new DateOnly(2026, 11, 26), MarketCatalog.LocalDate(Market.NewZealand, instant));
    }
}

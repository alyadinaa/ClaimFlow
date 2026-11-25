using ClaimFlow.Api.Modules.Claims.Entities;

namespace ClaimFlow.Api.Modules.Claims;

public record MarketInfo(string CurrencyCode, int DecimalPlaces, string TimeZoneId);

// Developer assumptions for the demo: illustrative markets, each with one representative time zone.
public static class MarketCatalog
{
    private static readonly Dictionary<Market, MarketInfo> Markets = new()
    {
        [Market.Malaysia] = new("MYR", 2, "Asia/Kuala_Lumpur"),
        [Market.Singapore] = new("SGD", 2, "Asia/Singapore"),
        [Market.Australia] = new("AUD", 2, "Australia/Sydney"),
        [Market.HongKong] = new("HKD", 2, "Asia/Hong_Kong"),
        [Market.Japan] = new("JPY", 0, "Asia/Tokyo"),
        [Market.NewZealand] = new("NZD", 2, "Pacific/Auckland")
    };

    public static MarketInfo Get(Market market) =>
        Markets.TryGetValue(market, out var info)
            ? info
            : throw new ClaimRuleException(ClaimRuleError.Validation, "Market is not supported.");

    public static int DecimalPlacesFor(string currencyCode) =>
        Markets.Values.First(m => m.CurrencyCode == currencyCode).DecimalPlaces;

    public static DateOnly LocalDate(Market market, DateTimeOffset instant)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(Get(market).TimeZoneId);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, timeZone).DateTime);
    }
}

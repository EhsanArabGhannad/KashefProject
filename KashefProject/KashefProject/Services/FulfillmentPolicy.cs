using Microsoft.Extensions.Options;

namespace KashefProject.Services;

public sealed class FulfillmentOptions
{
    public const string SectionName = "Fulfillment";

    public string OriginState { get; set; } = "VA";
    public long StandardShippingCents { get; set; } = 1500;
    public long FreeShippingThresholdCents { get; set; } = 15000;
    public bool AutomaticTaxEnabled { get; set; }
}

public sealed class FulfillmentPolicy(IOptions<FulfillmentOptions> configuredOptions)
{
    private readonly FulfillmentOptions options = configuredOptions.Value;

    public long StandardShippingCents => options.StandardShippingCents;
    public long FreeShippingThresholdCents => options.FreeShippingThresholdCents;
    public bool AutomaticTaxEnabled => options.AutomaticTaxEnabled;

    public long ShippingFor(long subtotalCents) =>
        subtotalCents >= options.FreeShippingThresholdCents ? 0 : options.StandardShippingCents;
}

namespace NetFinance.Models.DatahubIo;

public class SP500Instrument
{
	public string? Symbol { get; set; }
	public string? Name { get; set; }
	public string? Sector { get; set; }
	public double? Price { get; set; }
	public double? PriceEarnings { get; set; }
	public double? DividendYield { get; set; }
	public double? EarningsShare { get; set; }
	public double? num52WeekLow { get; set; }
	public double? num52WeekHigh { get; set; }
	public long? MarketCap { get; set; }
	public long? EBITDA { get; set; }
	public double? PriceSales { get; set; }
	public double? PriceBook { get; set; }
	public string? SECFilings { get; set; }
}

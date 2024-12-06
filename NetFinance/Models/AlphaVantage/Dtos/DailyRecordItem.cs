using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace NetFinance.Models.AlphaVantage.Dtos;
[ExcludeFromCodeCoverage]
internal record DailyRecordItem
{
	[JsonProperty("1. open")]
	public double Open { get; set; }

	[JsonProperty("2. high")]
	public double High { get; set; }

	[JsonProperty("3. low")]
	public double Low { get; set; }

	[JsonProperty("4. close")]
	public double Close { get; set; }

	[JsonProperty("5. adjusted close")]
	public double AdjustedClose { get; set; }

	[JsonProperty("6. volume")]
	public long Volume { get; set; }

	[JsonProperty("8. split coefficient")]
	public double SplitCoefficient { get; set; }
}

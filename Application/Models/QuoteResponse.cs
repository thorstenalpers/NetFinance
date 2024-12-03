using Newtonsoft.Json;

namespace NetFinance.Application.Models;

internal record QuoteResponse
{
	[JsonProperty("result")]
	public Quote[]? Result { get; set; }

	[JsonProperty("error")]
	public object? Error { get; set; }
}
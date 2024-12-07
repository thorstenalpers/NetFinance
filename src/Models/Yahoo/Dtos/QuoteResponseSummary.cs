using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace NetFinance.Models.Yahoo.Dtos;

[ExcludeFromCodeCoverage]
internal record QuoteResponseSummary
{
	[JsonProperty("result")]
	public QuoteResponse[]? Result { get; set; }

	[JsonProperty("error")]
	public object? Error { get; set; }
}
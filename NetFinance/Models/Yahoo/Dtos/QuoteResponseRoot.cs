using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace NetFinance.Models.Yahoo.Dtos;

[ExcludeFromCodeCoverage]
internal record QuoteResponseRoot
{
	[JsonProperty("quoteResponse")]
	public QuoteResponseSummary? QuoteResponse { get; set; }
}
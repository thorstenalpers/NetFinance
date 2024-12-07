
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace NetFinance.Models.AlphaVantage.Dtos;
[ExcludeFromCodeCoverage]
internal record DailyRecordRoot
{
	[JsonProperty("Meta Data")]
	public Dictionary<string, string>? MetaData { get; set; }

	[JsonProperty("Time Series (Daily)")]
	public Dictionary<DateTime, DailyRecordItem>? TimeSeries { get; set; }
}

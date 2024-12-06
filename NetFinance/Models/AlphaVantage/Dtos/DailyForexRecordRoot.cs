
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace NetFinance.Models.AlphaVantage.Dtos;
[ExcludeFromCodeCoverage]
internal record DailyForexRecordRoot
{
	[JsonProperty("Meta Data")]
	public Dictionary<string, string>? MetaData { get; set; }

	[JsonProperty("Time Series FX (Daily)")]
	public Dictionary<DateTime, DailyForexRecordItem>? TimeSeries { get; set; }
}

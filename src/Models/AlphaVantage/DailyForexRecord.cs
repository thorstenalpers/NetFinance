using System;

namespace NetFinance.Models.AlphaVantage;

public record DailyForexRecord
{
	public DateTime? Date { get; set; }
	public double? Open { get; set; }

	public double? High { get; set; }

	public double? Low { get; set; }

	public double? Close { get; set; }
}
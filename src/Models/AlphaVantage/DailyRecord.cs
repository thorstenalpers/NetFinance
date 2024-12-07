using System;

namespace NetFinance.Models.AlphaVantage;
public record DailyRecord
{
	public DateTime Date { get; set; }
	public double? Open { get; set; }

	public double? Low { get; set; }
	public double? High { get; set; }

	public double? Close { get; set; }

	public double? AdjustedClose { get; set; }

	public long? Volume { get; set; }

	public double? SplitCoefficient { get; set; }
}

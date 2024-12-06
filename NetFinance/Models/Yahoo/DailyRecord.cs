using System;

namespace NetFinance.Models.Yahoo;

public record DailyRecord
{
	public DateTime Date { get; set; }

	public decimal? Open { get; set; }
	public decimal? High { get; set; }
	public decimal? Low { get; set; }
	public decimal? Close { get; set; }
	public decimal? AdjustedClose { get; set; }
	public long? Volume { get; set; }
}

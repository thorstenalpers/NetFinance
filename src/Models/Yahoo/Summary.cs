using System;

namespace NetFinance.Models.Yahoo;

public record Summary
{
	public string? MarketTimeNotice { get; set; }
	public decimal? PreviousClose { get; set; }
	public decimal? Open { get; set; }
	public decimal? Bid { get; set; }
	public decimal? Ask { get; set; }
	public decimal? DaysRange_Min { get; set; }
	public decimal? DaysRange_Max { get; set; }
	public decimal? WeekRange52_Min { get; set; }
	public decimal? WeekRange52_Max { get; set; }
	public decimal? Volume { get; set; }
	public decimal? AvgVolume { get; set; }
	public decimal? MarketCap_Intraday { get; set; }
	public decimal? Beta_5Y_Monthly { get; set; }
	public decimal? PE_Ratio_TTM { get; set; }
	public decimal? EPS_TTM { get; set; }
	public DateTime? EarningsDate { get; set; }
	public decimal? Forward_Dividend { get; set; }
	public decimal? Forward_Yield { get; set; }
	public DateTime? Ex_DividendDate { get; set; }
	public decimal? OneYearTargetEst { get; set; }
}

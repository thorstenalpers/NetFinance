using System.ComponentModel;

namespace NetFinance.Models.AlphaVantage;

public enum EInterval
{
	[Description("1min")] Interval_1Min = 1,
	[Description("5min")] Interval_5Min = 2,
	[Description("15min")] Interval_15Min = 3,
	[Description("30min")] Interval_30Min = 4,
	[Description("60min")] Interval_60Min = 5,
}
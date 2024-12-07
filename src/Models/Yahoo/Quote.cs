using System;

namespace NetFinance.Models.Yahoo;

public record Quote
{
	public string? Language { get; set; }

	public string? Region { get; set; }

	public string? QuoteType { get; set; }

	public string? TypeDisp { get; set; }

	public string? QuoteSourceName { get; set; }

	public bool? Triggerable { get; set; }

	public string? CustomPriceAlertConfidence { get; set; }

	public string? Currency { get; set; }

	public string? Exchange { get; set; }

	public string? ShortName { get; set; }

	public string? LongName { get; set; }

	public string? MessageBoardId { get; set; }

	public string? ExchangeTimezoneName { get; set; }

	public string? ExchangeTimezoneShortName { get; set; }

	public long? GmtOffSetMilliseconds { get; set; }

	public string? Market { get; set; }

	public bool? EsgPopulated { get; set; }

	public double? RegularMarketChangePercent { get; set; }

	public double? RegularMarketPrice { get; set; }

	public string? MarketState { get; set; }

	public string? FullExchangeName { get; set; }

	public string? FinancialCurrency { get; set; }

	public double? RegularMarketOpen { get; set; }

	public long? AverageDailyVolume3Month { get; set; }

	public long? AverageDailyVolume10Day { get; set; }

	public double? FiftyTwoWeekLowChange { get; set; }

	public double? FiftyTwoWeekLowChangePercent { get; set; }

	public string? FiftyTwoWeekRange { get; set; }

	public double? FiftyTwoWeekHighChange { get; set; }

	public double? FiftyTwoWeekHighChangePercent { get; set; }

	public double? FiftyTwoWeekLow { get; set; }

	public double? FiftyTwoWeekHigh { get; set; }

	public double? FiftyTwoWeekChangePercent { get; set; }

	public DateTime? EarningsDate { get; set; }

	public DateTime? EarningsDateStart { get; set; }

	public DateTime? EarningsDateEnd { get; set; }

	public DateTime? EarningsCallDateStart { get; set; }

	public DateTime? EarningsCallDateEnd { get; set; }

	public bool? IsEarningsDateEstimate { get; set; }

	public double? TrailingAnnualDividendRate { get; set; }

	public double? TrailingPe { get; set; }

	public double? DividendRate { get; set; }
	public DateTime? DividendDate { get; set; }

	public double? TrailingAnnualDividendYield { get; set; }

	public double? DividendYield { get; set; }

	public double? EpsTrailingTwelveMonths { get; set; }

	public double? EpsForward { get; set; }

	public double? EpsCurrentYear { get; set; }

	public double? PriceEpsCurrentYear { get; set; }

	public long? SharesOutstanding { get; set; }

	public double? BookValue { get; set; }

	public double? FiftyDayAverage { get; set; }

	public double? FiftyDayAverageChange { get; set; }

	public double? PriceHint { get; set; }

	public double? PostMarketChangePercent { get; set; }

	public DateTime? PostMarketTime { get; set; }

	public double? PostMarketPrice { get; set; }

	public double? PostMarketChange { get; set; }

	public double? RegularMarketChange { get; set; }

	public DateTime? RegularMarketTime { get; set; }

	public double? RegularMarketDayHigh { get; set; }

	public string? RegularMarketDayRange { get; set; }

	public double? RegularMarketDayLow { get; set; }

	public long? RegularMarketVolume { get; set; }

	public double? RegularMarketPreviousClose { get; set; }

	public double? Bid { get; set; }

	public double? Ask { get; set; }

	public long? BidSize { get; set; }

	public long? AskSize { get; set; }

	public double? FiftyDayAverageChangePercent { get; set; }

	public double? TwoHundredDayAverage { get; set; }

	public double? TwoHundredDayAverageChange { get; set; }

	public double? TwoHundredDayAverageChangePercent { get; set; }

	public long? MarketCap { get; set; }

	public double? ForwardPe { get; set; }

	public double? PriceToBook { get; set; }
	public long? SourceInterval { get; set; }

	public long? ExchangeDataDelayedBy { get; set; }

	public string? AverageAnalystRating { get; set; }

	public bool? Tradeable { get; set; }

	public bool? CryptoTradeable { get; set; }

	public bool? HasPrePostMarketData { get; set; }

	public DateTime? FirstTradeDate { get; set; }

	public string? DisplayName { get; set; }

	public string? Symbol { get; set; }
}
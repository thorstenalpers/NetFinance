namespace NetFinance.Models.DatahubIo;

public record NasdaqInstrument
{
	public string? Symbol { get; set; }
	public string? CompanyName { get; set; }
	public string? SecurityName { get; set; }
	public string? MarketCategory { get; set; }
	public string? TestIssue { get; set; }
	public string? FinancialStatus { get; set; }
	public int? RoundLotSize { get; set; }
	public string? ETF { get; set; }
	public string? NextShares { get; set; }
}

namespace NetFinance.Models.Yahoo;

public record FinancialReport
{
	public decimal? TotalRevenue { get; set; }
	public decimal? CostOfRevenue { get; set; }
	public decimal? GrossProfit { get; set; }
	public decimal? OperatingExpense { get; set; }
	public decimal? OperatingIncome { get; set; }
	public decimal? NetNonOperatingInterestIncomeExpense { get; set; }
	public decimal? OtherIncomeExpense { get; set; }
	public decimal? PretaxIncome { get; set; }
	public decimal? TaxProvision { get; set; }
	public decimal? NetIncomeCommonStockholders { get; set; }
	public decimal? DilutedNIAvailableToComStockholders { get; set; }
	public decimal? BasicEPS { get; set; }
	public decimal? DilutedEPS { get; set; }
	public decimal? BasicAverageShares { get; set; }
	public decimal? DilutedAverageShares { get; set; }
	public decimal? TotalOperatingIncomeAsReported { get; set; }
	public decimal? TotalExpenses { get; set; }
	public decimal? NetIncomeFromContinuingAndDiscontinuedOperation { get; set; }
	public decimal? NormalizedIncome { get; set; }
	public decimal? InterestIncome { get; set; }
	public decimal? InterestExpense { get; set; }
	public decimal? NetInterestIncome { get; set; }
	public decimal? EBIT { get; set; }
	public decimal? EBITDA { get; set; }
	public decimal? ReconciledCostOfRevenue { get; set; }
	public decimal? ReconciledDepreciation { get; set; }
	public decimal? NetIncomeFromContinuingOperationNetMinorityInterest { get; set; }
	public decimal? TotalUnusualItemsExcludingGoodwill { get; set; }
	public decimal? TotalUnusualItems { get; set; }
	public decimal? NormalizedEBITDA { get; set; }
	public decimal? TaxRateForCalcs { get; set; }
	public decimal? TaxEffectOfUnusualItems { get; set; }
}
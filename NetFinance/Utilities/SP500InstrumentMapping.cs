using System.Diagnostics.CodeAnalysis;
using CsvHelper.Configuration;
using NetFinance.Models.OpenData;

namespace NetFinance.Utilities;

[ExcludeFromCodeCoverage]
public class SP500InstrumentMapping : ClassMap<SP500Instrument>
{
	public SP500InstrumentMapping()
	{
		Map(m => m.Symbol).Name("Symbol");
		Map(m => m.Name).Name("Name");
		Map(m => m.Sector).Name("Sector");
		Map(m => m.Price).Name("Price");
		Map(m => m.PriceEarnings).Name("Price/Earnings");
		Map(m => m.DividendYield).Name("Dividend Yield");
		Map(m => m.EarningsShare).Name("Earnings/Share");
		Map(m => m.num52WeekLow).Name("52 Week Low");
		Map(m => m.num52WeekHigh).Name("52 Week High");
		Map(m => m.MarketCap).Name("Market Cap");
		Map(m => m.EBITDA).Name("EBITDA");
		Map(m => m.PriceSales).Name("Price/Sales");
		Map(m => m.PriceBook).Name("Price/Book");
		Map(m => m.SECFilings).Name("SEC Filings");
	}
}

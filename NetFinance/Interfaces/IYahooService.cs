using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetFinance.Models.Yahoo;

namespace NetFinance.Interfaces;

/// <summary>
/// Represents a service for interacting with the Yahoo Finance API.
/// Provides methods for retrieving historical data, company profiles, summaries, and financial reports from Yahoo Finance.
/// </summary>
public interface IYahooService
{
	/// <summary>
	/// Retrieves a single quote's data from Yahoo Finance.
	/// </summary>
	/// <param name="symbol">The symbol of the quote (e.g., "AAPL" for Apple).</param>
	/// <param name="token">A <see cref="CancellationToken"/> to allow cancellation of the operation.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the quote for the given symbol.</returns>
	Task<Quote> GetQuoteAsync(string symbol, CancellationToken token = default);

	/// <summary>
	/// Retrieves multiple quotes from Yahoo Finance based on a list of symbols.
	/// </summary>
	/// <param name="symbols">A list of symbols for the quotes to retrieve data for.</param>
	/// <param name="token">A <see cref="CancellationToken"/> to allow cancellation of the operation.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains an enumerable list of quotes for the given symbols.</returns>
	Task<IEnumerable<Quote>> GetQuotesAsync(List<string> symbols, CancellationToken token = default);

	/// <summary>
	/// Retrieves historical data records for a specific quote from Yahoo Finance.
	/// </summary>
	/// <param name="symbol">The symbol of the quote (e.g., "AAPL" for Apple).</param>
	/// <param name="startDate">The start date for retrieving historical records.</param>
	/// <param name="endDate">Optional end date for retrieving historical records. If not provided, the current date will be used.</param>
	/// <param name="token">A <see cref="CancellationToken"/> to allow cancellation of the operation.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains an enumerable of YahooRecord objects for the given symbol and date range.</returns>
	Task<IEnumerable<DailyRecord>> GetDailyRecordsAsync(string symbol, DateTime startDate, DateTime? endDate = null, CancellationToken token = default);

	/// <summary>
	/// Retrieves the profile data for a specific symbol from Yahoo Finance.
	/// </summary>
	/// <param name="symbol">The symbol of the quote (e.g., "AAPL" for Apple).</param>
	/// <param name="token">A <see cref="CancellationToken"/> to allow cancellation of the operation.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the profile data for the given symbol.</returns>
	public Task<Profile> GetProfileAsync(string symbol, CancellationToken token = default);

	/// <summary>
	/// Retrieves the summary data for a specific symbol from Yahoo Finance.
	/// </summary>
	/// <param name="symbol">The symbol of the quote (e.g., "AAPL" for Apple).</param>
	/// <param name="token">A <see cref="CancellationToken"/> to allow cancellation of the operation.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the summary data for the given symbol.</returns>
	public Task<Summary> GetSummaryAsync(string symbol, CancellationToken token = default);

	/// <summary>
	/// Retrieves financial reports for a specific symbol from Yahoo Finance.
	/// </summary>
	/// <param name="symbol">The symbol of the quote (e.g., "AAPL" for Apple).</param>
	/// <param name="token">A <see cref="CancellationToken"/> to allow cancellation of the operation.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains a dictionary of financial reports for the given symbol.</returns>
	Task<Dictionary<string, FinancialReport>> GetFinancialReportsAsync(string symbol, CancellationToken token = default);
}
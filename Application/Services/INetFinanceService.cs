using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetFinance.Application.Models;

namespace NetFinance.Application.Services;

public interface INetFinanceService
{
	/// <summary>
	/// Asynchronously retrieves a single quote's data from Yahoo Finance.
	/// </summary>
	/// <param name="symbol">The symbol of the quote (e.g., "AAPL" for Apple).</param>
	/// <param name="maxAttempts">Optional max retry attempts. If not provided, 3 reties with 3 seconds delay between.</param>
	/// <param name="token">A <see cref="CancellationToken"/> to allow cancellation of the operation.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the quote for the given symbol.</returns>
	public Task<Quote> GetQuoteAsync(string symbol, int maxAttempts = 3, CancellationToken token = default);

	/// <summary>
	/// Asynchronously retrieves multiple quotes from Yahoo Finance based on a list of symbols.
	/// </summary>
	/// <param name="symbols">A list of symbols for the quotes to retrieve data for.</param>
	/// <param name="maxAttempts">Optional max retry attempts. If not provided, 3 reties with 3 seconds delay between.</param>
	/// <param name="token">A <see cref="CancellationToken"/> to allow cancellation of the operation.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains an enumerable list of quotes for the given symbols.</returns>
	public Task<IEnumerable<Quote>> GetQuotesAsync(List<string> symbols, int maxAttempts = 3, CancellationToken token = default);

	/// <summary>
	/// Asynchronously retrieves historical data records for a specific quote from Yahoo Finance.
	/// </summary>
	/// <param name="symbol">The symbol of the quote (e.g., "AAPL" for Apple).</param>
	/// <param name="startDate">The start date for retrieving historical records.</param>
	/// <param name="endDate">Optional end date for retrieving historical records. If not provided, the current date will be used.</param>
	/// <param name="maxAttempts">Optional max retry attempts. If not provided, 3 reties with 3 seconds delay between.</param>
	/// <param name="token">A <see cref="CancellationToken"/> to allow cancellation of the operation.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains an enumerable of YahooRecord objects for the given symbol and date range.</returns>
	public Task<IEnumerable<Record>> GetRecordsAsync(string symbol, DateTime startDate, DateTime? endDate = null, int maxAttempts = 3, CancellationToken token = default);
}
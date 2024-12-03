using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetFinance.Application.Exceptions;
using NetFinance.Application.Models;
using NetFinance.Application.Utilities;
using Newtonsoft.Json;

namespace NetFinance.Application.Services;

public class NetFinanceService(ILogger<NetFinanceService> logger,
							   IHttpClientFactory httpClientFactory,
							   IYahooSession yahooSession) : INetFinanceService
{
	private readonly ILogger<NetFinanceService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
	private readonly IYahooSession _yahooSession = yahooSession ?? throw new ArgumentNullException(nameof(yahooSession));

	public async Task<Quote> GetQuoteAsync(string symbol, int maxAttempts = 3, CancellationToken token = default)
	{
		var quotes = await GetQuotesAsync([symbol], maxAttempts, token).ConfigureAwait(false);
		var quote = quotes.FirstOrDefault(e => e.Symbol == symbol) ?? throw new NetFinanceException($"No security for symbol {symbol} found");
		return quote;
	}

	public async Task<IEnumerable<Quote>> GetQuotesAsync(List<string> symbols, int maxAttempts = 3, CancellationToken token = default)
	{
		var quotes = new List<Quote>();
		var (crumb, cookie) = await _yahooSession.GetSessionStateAsync(token).ConfigureAwait(false);

		var httpClient = _httpClientFactory.CreateClient(Constants.ApiClientName);

		var url = $"{Constants.BaseUrl_Quote_Api}?" +
			$"&symbols={string.Join(",", symbols)}" +
			$"&crumb={crumb}";

		var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
		requestMessage.Headers.Add("Cookie", $"{cookie.Name}={cookie.Value}");

		Exception? lastException = null;
		for (int attempt = 1; attempt <= maxAttempts; attempt++)
		{
			try
			{
				var response = await httpClient.SendAsync(requestMessage, token).ConfigureAwait(false);
				response.EnsureSuccessStatusCode();

				var data = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

				var parsedData = JsonConvert.DeserializeObject<QuoteResponseRoot>(data) ?? throw new NetFinanceException($"Invalid data returned by Yahoo");
				var responseObj = parsedData.QuoteResponse ?? throw new NetFinanceException($"Unexpected response from Yahoo");

				var error = responseObj.Error;
				if (responseObj == null || error != null)
				{
					throw new NetFinanceException($"An error returned by Yahoo: {error}");
				}
				if (responseObj.Result == null)
				{
					return quotes;
				}

				foreach (var quote in responseObj.Result)
				{
					if (quote.Symbol == null)
					{
						throw new NetFinanceException("Invalid quote field symbol");
					}
					quotes.Add(quote);
				}
				return quotes;
			}
			catch (Exception ex)
			{
				await Task.Delay(TimeSpan.FromSeconds(3));
				lastException = ex;
			}
		}
		throw new NetFinanceException($"No quote found after {maxAttempts} attempts", lastException ?? new Exception());
	}

	public async Task<IEnumerable<Record>> GetRecordsAsync(string symbol, DateTime startDate, DateTime? endDate = null, int maxAttempts = 3, CancellationToken token = default)
	{
		var symbolsToSecurity = new Dictionary<string, Quote>();
		var (crumb, cookie) = await _yahooSession.GetSessionStateAsync(token).ConfigureAwait(false);
		var httpClient = _httpClientFactory.CreateClient(Constants.ApiClientName);

		endDate ??= DateTime.UtcNow;
		endDate = endDate.Value.AddDays(1).Date;

		long period1 = Helper.ToUnixTimestamp(startDate.Date);
		long period2 = Helper.ToUnixTimestamp(endDate.Value.Date);
		var url = $"{Constants.BaseUrl_Html}/{symbol}/history/?period1={period1}&period2={period2}";
		Exception? lastException = null;
		for (int attempt = 1; attempt <= maxAttempts; attempt++)
		{
			try
			{
				var records = new List<Record>();
				var expectedHeaders = new[] { "Date", "Open", "High", "Low", "Close", "Adj Close", "Volume" };
				var expectedHeaderSet = new HashSet<string>(expectedHeaders);
				var headerMap = new Dictionary<string, int>();

				var response = await httpClient.GetAsync(url, token).ConfigureAwait(false);
				response.EnsureSuccessStatusCode();

				var htmlContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
				var document = new AngleSharp.Html.Parser.HtmlParser().ParseDocument(htmlContent);

				var table = document.QuerySelector("table.table");
				if (table == null)
				{
					_logger.LogWarning($"No records found for symbol {symbol}");
					//throw new NetFinanceException("No records found");
					return records;
				}

				var headers = table.QuerySelectorAll("thead th")
					.Select(th =>
					{
						th.QuerySelectorAll("span").ToList().ForEach(span => span.Remove());
						return th.TextContent.Trim();
					})
					.ToList();
				for (int i = 0; i < headers.Count; i++)
				{
					headerMap[headers[i]] = i;
				}
				if (!expectedHeaderSet.IsSubsetOf(headerMap.Keys))
				{
					throw new NetFinanceException("Headers are missing");
				}

				var rows = table.QuerySelectorAll("tbody tr");
				foreach (var row in rows)
				{
					var cells = row.QuerySelectorAll("td").Select(td => td.TextContent.Trim()).ToArray();

					if (cells.Length == 7)
					{
						var dateString = cells[headerMap["Date"]];
						var openString = cells[headerMap["Open"]];
						var highString = cells[headerMap["High"]];
						var lowString = cells[headerMap["Low"]];
						var closeString = cells[headerMap["Close"]];
						var adjCloseString = cells[headerMap["Adj Close"]];
						var volumeString = cells[headerMap["Volume"]];

						var date = Helper.ParseDate(dateString);
						var open = Helper.ParseDecimal(openString);
						var high = Helper.ParseDecimal(highString);
						var low = Helper.ParseDecimal(lowString);
						var close = Helper.ParseDecimal(closeString);
						var adjClose = Helper.ParseDecimal(adjCloseString);
						var volume = Helper.ParseLong(volumeString);

						records.Add(new Record
						{
							Date = date,
							Open = open,
							High = high,
							Low = low,
							Close = close,
							AdjustedClose = adjClose,
							Volume = volume
						});
					}
					else
					{
						_logger.LogInformation($"No records in row {row.TextContent}");    // e.g. date + dividend (over all columns)
					}
				}
				return records;
			}
			catch (Exception ex)
			{
				await Task.Delay(TimeSpan.FromSeconds(3));
				lastException = ex;
			}
		}
		throw new NetFinanceException($"No records found after {maxAttempts} attempts", lastException ?? new Exception());
	}
}
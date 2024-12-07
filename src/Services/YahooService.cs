using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.XPath;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetFinance.Exceptions;
using NetFinance.Extensions;
using NetFinance.Interfaces;
using NetFinance.Models.Yahoo;
using NetFinance.Models.Yahoo.Dtos;
using NetFinance.Utilities;
using Newtonsoft.Json;

namespace NetFinance.Services;

internal class YahooService : IYahooService
{
	private readonly ILogger<IYahooService> _logger;
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly IYahooSession _yahooSession;
	private readonly IMapper _mapper;
	private readonly NetFinanceConfiguration _options;
	private static ServiceProvider? _serviceProvider = null;

	public YahooService(ILogger<IYahooService> logger, IHttpClientFactory httpClientFactory, IYahooSession yahooSession, IOptions<NetFinanceConfiguration> options)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
		_options = options?.Value ?? throw new ArgumentNullException(nameof(options));
		_yahooSession = yahooSession ?? throw new ArgumentNullException(nameof(yahooSession));

		// do not use IoC, so users can use Automapper independently
		var config = new MapperConfiguration(cfg => cfg.AddProfile<YahooQuoteAutomapperProfile>());
		_mapper = config.CreateMapper();
	}

	/// <summary>
	/// Creates a service for interacting with the Yahoo Finance API.
	/// Provides methods for retrieving historical data, company profiles, summaries, and financial reports from Yahoo Finance.
	/// </summary>
	/// <param name="cfg">Optional: Default values to configure .Net Finance. <see cref="NetFinanceConfiguration"/> ></param>
	public static IYahooService Create(NetFinanceConfiguration? cfg = null)
	{
		if (_serviceProvider == null)
		{
			var services = new ServiceCollection();
			services.AddNetFinance(cfg);
			_serviceProvider = services.BuildServiceProvider();
		}
		return _serviceProvider.GetRequiredService<IYahooService>();
	}

	public async Task<Quote> GetQuoteAsync(string symbol, CancellationToken token = default)
	{
		var symbols = await GetQuotesAsync([symbol], token);
		return symbols.FirstOrDefault(e => e.Symbol == symbol);
	}

	public async Task<IEnumerable<Quote>> GetQuotesAsync(List<string> symbols, CancellationToken token = default)
	{
		await _yahooSession.RefreshSessionAsync(token).ConfigureAwait(false);
		var httpClient = _httpClientFactory.CreateClient(_options.Yahoo_Http_ClientName);
		var url = $"{_options.Yahoo_BaseUrl_Quote_Api}?" +
			$"&symbols={string.Join(",", symbols)}" +
			$"&crumb={_yahooSession.GetCrumb()}";
		Exception? lastException = null;
		for (int attempt = 1; attempt <= _options.Http_Retries; attempt++)
		{
			try
			{
				var quotes = new List<Quote>();
				var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
				if (attempt == 1)
				{
					requestMessage.AddCookiesToRequest(_yahooSession.GetCookieCollection());
				}
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

				foreach (var quoteResponse in responseObj.Result)
				{
					if (quoteResponse.Symbol == null)
					{
						throw new NetFinanceException("Invalid quote field symbol");
					}
					var quote = _mapper.Map<Quote>(quoteResponse);
					quotes.Add(quote);
				}
				return quotes;
			}
			catch (Exception ex)
			{
				_logger.LogInformation($"Retry after exception {ex}");
				await Task.Delay(TimeSpan.FromSeconds(_options.Http_Retries_Waittime), token);
				lastException = ex;
			}
		}
		_logger.LogWarning($"No quotes found after {_options.Http_Retries} attempts.");
		return [];
	}

	public async Task<Models.Yahoo.Profile> GetProfileAsync(string symbol, CancellationToken token = default)
	{
		var httpClient = _httpClientFactory.CreateClient(_options.Yahoo_Http_ClientName);
		await _yahooSession.RefreshSessionAsync(token).ConfigureAwait(false);
		Exception? lastException = null;
		var url = $"{_options.Yahoo_BaseUrl_Html}/{symbol}/profile/";

		for (int attempt = 1; attempt <= _options.Http_Retries; attempt++)
		{
			try
			{
				var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
				if (attempt == 1)
				{
					requestMessage.AddCookiesToRequest(_yahooSession.GetCookieCollection());
				}
				var response = await httpClient.SendAsync(requestMessage, token).ConfigureAwait(false);
				response.EnsureSuccessStatusCode();
				var htmlContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
				var len = htmlContent.Length;

				var document = new AngleSharp.Html.Parser.HtmlParser().ParseDocument(htmlContent);

				var descriptionElement = document.Body.SelectSingleNode("//section[header/h3[contains(text(), 'Description')]]/p");
				var corporateGovernanceElements = document.Body.SelectNodes("//section[header/h3[contains(text(), 'Corporate Governance')]]/div");
				var cntEmployeesElement = document.Body.SelectSingleNode("//dt[contains(text(), 'Employees')]/following-sibling::dd");
				var industryElement = document.Body.SelectSingleNode("//dt[contains(text(), 'Industry')]/following-sibling::a");
				var sectorElement = document.Body.SelectSingleNode("//dt[contains(text(), 'Sector')]/following-sibling::dd/a");
				var phoneElement = document.Body.SelectSingleNode("//a[@aria-label='phone number']");
				var websiteElement = document.Body.SelectSingleNode("//a[@aria-label='website link']");
				var addressElements = document.Body.SelectNodes("//div[contains(@class, 'address')]/div");
				var addressNameElement = document.Body.SelectSingleNode("//div[contains(@class, 'address')]/../../..//h3");

				var description = descriptionElement?.TextContent?.Trim();
				var corporateGovernance = corporateGovernanceElements.IsNullOrEmpty() ? null : string.Join("\n", corporateGovernanceElements.Select(div => div.TextContent.Trim()).Where(e => !string.IsNullOrWhiteSpace(e)));
				var cntEmployees = cntEmployeesElement?.TextContent?.Replace(",", "").Replace("-", "")?.Trim();
				var industry = industryElement?.TextContent?.Trim();
				var sector = sectorElement?.TextContent?.Trim();
				var phone = phoneElement?.TextContent;
				var website = websiteElement?.TextContent?.Trim();
				var addressLocation = addressElements.IsNullOrEmpty() ? null : string.Join("\n", addressElements.Select(div => div.TextContent.Trim()));
				var addressName = addressNameElement?.TextContent?.Trim();
				var address = string.IsNullOrEmpty(addressName) ? addressLocation : addressName + "\n" + addressLocation;
				var cntEmployeesNumber = Helper.ParseLong(cntEmployees);

				var result = new Models.Yahoo.Profile
				{
					Description = description,
					CorporateGovernance = corporateGovernance,
					CntEmployees = cntEmployeesNumber,
					Industry = industry,
					Sector = sector,
					Adress = address,
					Phone = phone,
					Website = website
				};
				if (Helper.AreAllFieldsNull(result))
				{
					throw new NetFinanceException("All fields empty");
				}
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogInformation($"Retry after exception {ex}");
				await Task.Delay(TimeSpan.FromSeconds(_options.Http_Retries_Waittime), token);
				lastException = ex;

				// try using without cookies
				url = $"{_options.Yahoo_BaseUrl_Html}/{symbol}/profile/?_guc_consent_skip={Helper.ToUnixTimestamp(DateTime.UtcNow.AddHours(1))}";
			}
		}
		_logger.LogWarning($"No profile found after {_options.Http_Retries} attempts.");
		return new Models.Yahoo.Profile();
	}

	public async Task<IEnumerable<DailyRecord>> GetDailyRecordsAsync(string symbol, DateTime startDate, DateTime? endDate = null, CancellationToken token = default)
	{
		Exception? lastException = null;
		var httpClient = _httpClientFactory.CreateClient(_options.Yahoo_Http_ClientName);
		await _yahooSession.RefreshSessionAsync(token).ConfigureAwait(false);

		endDate ??= DateTime.UtcNow;
		endDate = endDate.Value.AddDays(1).Date;

		var period1 = Helper.ToUnixTimestamp(startDate.Date) ?? throw new NetFinanceException("Invalid startDate");
		var period2 = Helper.ToUnixTimestamp(endDate.Value.Date) ?? throw new NetFinanceException("Invalid endDate");

		var url = $"{_options.Yahoo_BaseUrl_Html}/{symbol}/history/?period1={period1}&period2={period2}";
		for (int attempt = 1; attempt <= _options.Http_Retries; attempt++)
		{
			try
			{
				var records = new List<DailyRecord>();
				var expectedHeaders = new[] { "Date", "Open", "High", "Low", "Close", "Adj Close", "Volume" };
				var expectedHeaderSet = new HashSet<string>(expectedHeaders);
				var headerMap = new Dictionary<string, int>();

				var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
				if (attempt == 1)
				{
					requestMessage.AddCookiesToRequest(_yahooSession.GetCookieCollection());
				}
				var response = await httpClient.SendAsync(requestMessage, token).ConfigureAwait(false);
				response.EnsureSuccessStatusCode();

				var htmlContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
				var document = new AngleSharp.Html.Parser.HtmlParser().ParseDocument(htmlContent);

				var table = document.QuerySelector("table.table");
				if (table == null)
				{
					throw new NetFinanceException("No records found");
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

						if (date == null)
						{
							_logger.LogWarning($"invalid date {dateString}");
							continue;
						}

						records.Add(new DailyRecord
						{
							Date = date.Value,
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
				_logger.LogInformation($"Retry after exception {ex}");
				await Task.Delay(TimeSpan.FromSeconds(_options.Http_Retries_Waittime), token);
				lastException = ex;

				// try using without cookies
				url = $"{_options.Yahoo_BaseUrl_Html}/{symbol}/history/?period1={period1}&period2={period2}&_guc_consent_skip={Helper.ToUnixTimestamp(DateTime.UtcNow.AddHours(1))}";
			}
		}
		_logger.LogWarning($"No records found after {_options.Http_Retries} attempts.");
		return [];
	}

	public async Task<Dictionary<string, FinancialReport>> GetFinancialReportsAsync(string symbol, CancellationToken token = default)
	{
		Exception? lastException = null;
		var httpClient = _httpClientFactory.CreateClient(_options.Yahoo_Http_ClientName);
		await _yahooSession.RefreshSessionAsync(token).ConfigureAwait(false);
		var url = $"{_options.Yahoo_BaseUrl_Html}/{symbol}/financials/";
		for (int attempt = 1; attempt <= _options.Http_Retries; attempt++)
		{
			try
			{
				var result = new Dictionary<string, FinancialReport>();

				var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
				if (attempt == 1)
				{
					requestMessage.AddCookiesToRequest(_yahooSession.GetCookieCollection());
				}

				var response = await httpClient.SendAsync(requestMessage, token).ConfigureAwait(false);
				response.EnsureSuccessStatusCode();

				var htmlContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
				var document = new AngleSharp.Html.Parser.HtmlParser().ParseDocument(htmlContent);

				var headers = document
					.Body.SelectNodes("//div[contains(@class, 'tableHeader')]//div[contains(@class, 'column')]")
					.Select(header => header.TextContent.Trim())
					.Where(e => e != "Breakdown")
					.ToList();

				foreach (var header in headers)
				{
					result.Add(header, new FinancialReport());
				}

				var rows = document
					.Body.SelectNodes("//div[contains(@class, 'tableBody')]//div[contains(@class, 'row ')]")
					.ToList();

				foreach (var row in rows)
				{
					var columns = row.ChildNodes.QuerySelectorAll("div.column").Select(e => e.TextContent.Trim()).ToList();

					if (columns.Count != headers.Count + 1)
					{
						throw new NetFinanceException($"Unknown table format of {url} html");
					}

					var rowTitle = columns.FirstOrDefault();
					var values = columns.Skip(1).Select(Helper.ParseDecimal).ToList();

					string propertyName = rowTitle.Replace(" ", "").Replace("&", "And");
					var propertyInfo = typeof(FinancialReport).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

					if (propertyInfo != null)
					{
						for (int i = 0; i < headers.Count; i++)
						{
							var header = headers[i];
							var value = values[i];
							var report = result[header];
							propertyInfo.SetValue(report, value);
						}
					}
					else
					{
						_logger.LogWarning($"Unknown row property {rowTitle}.");
					}
				}
				if (result == null || result.Count == 0)
				{
					throw new NetFinanceException("no reports");
				}

				return result;
			}
			catch (Exception ex)
			{
				_logger.LogInformation($"Retry after exception {ex}");
				await Task.Delay(TimeSpan.FromSeconds(_options.Http_Retries_Waittime), token);
				lastException = ex;

				// try using without cookies
				url = $"{_options.Yahoo_BaseUrl_Html}/{symbol}/financials/?_guc_consent_skip={Helper.ToUnixTimestamp(DateTime.UtcNow.AddHours(1))}";
			}
		}
		_logger.LogWarning($"No financial reports found after {_options.Http_Retries} attempts.");
		return [];
	}

	public async Task<Summary> GetSummaryAsync(string symbol, CancellationToken token = default)
	{
		var httpClient = _httpClientFactory.CreateClient(_options.Yahoo_Http_ClientName);
		await _yahooSession.RefreshSessionAsync(token).ConfigureAwait(false);
		Exception? lastException = null;
		var symbolsToSecurity = new Dictionary<string, Quote>();
		var url = $"{_options.Yahoo_BaseUrl_Html}/{symbol}/";

		for (int attempt = 1; attempt <= _options.Http_Retries; attempt++)
		{
			try
			{
				var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
				if (attempt == 1)
				{
					requestMessage.AddCookiesToRequest(_yahooSession.GetCookieCollection());
				}
				var response = await httpClient.SendAsync(requestMessage, token).ConfigureAwait(false);
				response.EnsureSuccessStatusCode();

				var htmlContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
				var document = new AngleSharp.Html.Parser.HtmlParser().ParseDocument(htmlContent);

				var askElement = document.Body.SelectSingleNode("//li[span[contains(text(), 'Ask')]]/span[2]");
				var askStr = askElement?.TextContent?.Trim();
				askStr = Regex.Replace(askStr, @"\s*x\s*[0-9 -]+", "")?.Trim();  // remove "x 100" of e.g. "415.81 x 100"
				var ask = Helper.ParseDecimal(askStr);

				var avgVolumeElement = document.Body.SelectSingleNode("//li[span[contains(text(), 'Avg. Volume')]]/span[2]");
				var avgVolume = Helper.ParseDecimal(avgVolumeElement?.TextContent?.Trim());

				var beta_5Y_MonthlyElement = document.Body.SelectSingleNode("//li[span[contains(text(), 'Beta (5Y Monthly)')]]/span[2]");
				var beta_5Y_Monthly = Helper.ParseDecimal(beta_5Y_MonthlyElement?.TextContent?.Trim());

				var bidElement = document.Body.SelectSingleNode("//li[span[contains(text(), 'Bid')]]/span[2]");
				var bidStr = bidElement?.TextContent?.Trim();
				bidStr = Regex.Replace(bidStr, @"\s*x\s*[0-9 -]+", "")?.Trim();  // remove "x 100" of e.g. "415.81 x 100"
				var bid = Helper.ParseDecimal(bidStr);

				var daysRangeElement = document.Body.SelectSingleNode("//li[span[contains(text(), 's Range')]]/span[2]");
				var daysRange = daysRangeElement?.TextContent?.Trim()?.Split(" - ");
				var daysRange_Min = daysRange?.Count() == 2 ? Helper.ParseDecimal(daysRange.FirstOrDefault()) : null;
				var daysRange_Max = daysRange?.Count() == 2 ? Helper.ParseDecimal(daysRange.LastOrDefault()) : null;

				var earningsDateElement = document.Body.SelectSingleNode("//li[span[contains(text(), 'Earnings Date')]]/span[2]");
				var earningsDateStr = earningsDateElement?.TextContent?.Trim()?.Split(" - ");
				var earningsDate = earningsDateStr == null || !earningsDateStr.Any() ? null : Helper.ParseDate(earningsDateStr.FirstOrDefault());

				var ePS_TTMElement = document.Body.SelectSingleNode("//li[span[contains(text(), 'EPS (TTM)')]]/span[2]");
				var ePS_TTM = Helper.ParseDecimal(ePS_TTMElement?.TextContent?.Trim());

				var ex_DividendDateElement = document.Body.SelectSingleNode("//li[span[contains(text(), 'Ex-Dividend Date')]]/span[2]");
				var ex_DividendDate = Helper.ParseDate(ex_DividendDateElement?.TextContent?.Trim());

				var forward_DividendAndYieldElement = document.Body.SelectSingleNode("//li[span[contains(text(), 'Forward Dividend & Yield')]]/span[2]");
				var dividentAndYield = forward_DividendAndYieldElement?.TextContent?.Trim().Split(" ")?.Select(e => e.Replace("(", "").Replace(")", "").Replace("%", ""));
				var forward_Dividend = dividentAndYield?.Count() == 2 ? Helper.ParseDecimal(dividentAndYield.FirstOrDefault()) : null;
				var forward_Yield = dividentAndYield?.Count() == 2 ? Helper.ParseDecimal(dividentAndYield.LastOrDefault()) : null;

				var marketCap_IntradayElement = document.Body.SelectSingleNode("//li[span[contains(text(), 'Market Cap (intraday)')]]/span[2]");
				var marketCap_Intraday = Helper.ParseDecimal(marketCap_IntradayElement?.TextContent?.Trim());

				var marketTimeNoticeElement = document.Body.SelectSingleNode("//div[@slot='marketTimeNotice']");
				var marketTimeNotice = marketTimeNoticeElement?.TextContent?.Trim();

				var oneYearTargetEstElement = document.Body.SelectSingleNode("//li[span[contains(text(), '1y Target Est')]]/span[2]");
				var oneYearTargetEst = Helper.ParseDecimal(oneYearTargetEstElement?.TextContent?.Trim());

				var openElement = document.Body.SelectSingleNode("//li[span[contains(text(), 'Open')]]/span[2]");
				var open = Helper.ParseDecimal(openElement?.TextContent?.Trim());

				var pE_Ratio_TTMElement = document.Body.SelectSingleNode("//li[span[contains(text(), 'PE Ratio (TTM)')]]/span[2]");
				var pE_Ratio_TTM = Helper.ParseDecimal(pE_Ratio_TTMElement?.TextContent?.Trim());

				var previousCloseElement = document.Body.SelectSingleNode("//li[span[contains(text(), 'Previous Close')]]/span[2]");
				var previousClose = Helper.ParseDecimal(previousCloseElement?.TextContent?.Trim());

				var volumeElement = document.Body.SelectSingleNode("//li[span[contains(text(), 'Volume')]]/span[2]");
				var volume = Helper.ParseDecimal(volumeElement?.TextContent?.Trim());

				var weekRange52Element = document.Body.SelectSingleNode("//li[span[contains(text(), '52 Week Range')]]/span[2]");
				var weekRange52 = weekRange52Element?.TextContent?.Trim()?.Split(" - ");
				var weekRange52_Min = weekRange52?.Count() == 2 ? Helper.ParseDecimal(weekRange52.FirstOrDefault()) : null;
				var weekRange52_Max = weekRange52?.Count() == 2 ? Helper.ParseDecimal(weekRange52.LastOrDefault()) : null;

				var result = new Summary
				{
					Ask = ask,
					AvgVolume = avgVolume,
					Beta_5Y_Monthly = beta_5Y_Monthly,
					Bid = bid,
					DaysRange_Max = daysRange_Max,
					DaysRange_Min = daysRange_Min,
					EarningsDate = earningsDate,
					EPS_TTM = ePS_TTM,
					Ex_DividendDate = ex_DividendDate,
					Forward_Dividend = forward_Dividend,
					Forward_Yield = forward_Yield,
					MarketCap_Intraday = marketCap_Intraday,
					MarketTimeNotice = marketTimeNotice,
					OneYearTargetEst = oneYearTargetEst,
					Open = open,
					PE_Ratio_TTM = pE_Ratio_TTM,
					PreviousClose = previousClose,
					Volume = volume,
					WeekRange52_Max = weekRange52_Max,
					WeekRange52_Min = weekRange52_Min
				};
				if (Helper.AreAllFieldsNull(result))
				{
					throw new NetFinanceException("All fields empty");
				}
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogInformation($"Retry after exception {ex}");
				await Task.Delay(TimeSpan.FromSeconds(_options.Http_Retries_Waittime), token);
				lastException = ex;

				// try using without cookies
				url = $"{_options.Yahoo_BaseUrl_Html}/{symbol}/?_guc_consent_skip={Helper.ToUnixTimestamp(DateTime.UtcNow.AddHours(1))}";
			}
		}
		_logger.LogWarning($"No summary found after {_options.Http_Retries} attempts.");
		return new Summary();
	}
}
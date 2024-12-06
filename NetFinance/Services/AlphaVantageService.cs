using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetFinance.Exceptions;
using NetFinance.Extensions;
using NetFinance.Interfaces;
using NetFinance.Models.AlphaVantage;
using NetFinance.Models.AlphaVantage.Dtos;
using NetFinance.Utilities;
using Newtonsoft.Json;

namespace NetFinance.Services;

internal class AlphaVantageService : IAlphaVantageService
{
	private readonly ILogger<IAlphaVantageService> _logger;
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly NetFinanceConfiguration _options;
	private static ServiceProvider? _serviceProvider = null;

	public AlphaVantageService(ILogger<IAlphaVantageService> logger, IHttpClientFactory httpClientFactory, IOptions<NetFinanceConfiguration> options)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
		_options = options?.Value ?? throw new ArgumentNullException(nameof(options));
	}

	/// <summary>
	/// Creates a service for interacting with the AlphaVantage API.
	/// Provides methods for retrieving company data, stock records, forex data, and intraday information.
	/// </summary>
	/// <param name="cfg">Optional: Default values to configure .Net Finance. <see cref="NetFinanceConfiguration"/> ></param>
	public static IAlphaVantageService Create(NetFinanceConfiguration? cfg = null)
	{
		if (_serviceProvider == null)
		{
			var services = new ServiceCollection();
			services.AddNetFinance(cfg);
			_serviceProvider = services.BuildServiceProvider();
		}
		return _serviceProvider.GetRequiredService<IAlphaVantageService>();
	}

	public async Task<CompanyInfo?> GetCompanyInfoAsync(string symbol, CancellationToken token = default)
	{
		using var httpClient = _httpClientFactory.CreateClient(_options.AlphaVantage_Http_ClientName);
		Exception? lastException = new();
		for (int index = 0; index < _options.Http_Retries; index++)
		{
			try
			{
				var requestUrl = _options.AlphaVantage_ApiUrl + "/query?function=OVERVIEW" +
					$"&symbol={symbol}" +
					$"&apikey={_options.AlphaVantageApiKey}";

				var response = await httpClient.GetAsync(requestUrl, token);

				if (response.StatusCode == HttpStatusCode.NotFound)
				{
					throw new NetFinanceException($"No company overview available for {symbol}");
				}
				response.EnsureSuccessStatusCode();

				string jsonResponse = await response.Content.ReadAsStringAsync();
				if (jsonResponse.Contains("higher API call volume"))
				{
					throw new NetFinanceException($"higher API call volume for {symbol}");
				}
				return JsonConvert.DeserializeObject<CompanyInfo>(jsonResponse);
			}
			catch (Exception ex)
			{
				_logger.LogInformation($"Retry for {symbol}");
				lastException = ex;
				await Task.Delay(TimeSpan.FromSeconds(10), token);
			}
		}
		throw new NetFinanceException($"No company overview found for {symbol} after {_options.Http_Retries} retries", lastException);
	}

	public async Task<IEnumerable<DailyRecord>> GetDailyRecordsAsync(string symbol, DateTime startDate, DateTime? endDate = null, CancellationToken token = default)
	{
		using var httpClient = _httpClientFactory.CreateClient(_options.AlphaVantage_Http_ClientName);
		var lastException = new Exception();
		Guard.Against.NullOrEmpty(symbol);
		var result = new List<DailyRecord>();
		if (endDate == null || endDate?.Date >= DateTime.UtcNow.Date)
		{
			endDate = DateTime.UtcNow.Date;
		}
		if (startDate > endDate)
		{
			throw new NetFinanceException("Startdate after Endate");
		}
		var daysToImport = ((endDate ?? startDate) - startDate).TotalDays;
		for (int retryAttempt = 0; retryAttempt < _options.Http_Retries; retryAttempt++)
		{
			try
			{
				var requestUrl = _options.AlphaVantage_ApiUrl + "/query?function=TIME_SERIES_DAILY_ADJUSTED" +
					$"&symbol={symbol}&outputsize=full&apikey={_options.AlphaVantageApiKey}";

				var response = await httpClient.GetAsync(requestUrl, token);

				if (response.StatusCode == HttpStatusCode.NotFound)
				{
					throw new NetFinanceException($"No daily records found for {symbol}");
				}
				response.EnsureSuccessStatusCode();

				string jsonResponse = await response.Content.ReadAsStringAsync();
				if (jsonResponse.Contains("higher API call volume"))
				{
					throw new NetFinanceException($"higher API call volume for {symbol}");
				}

				var data = JsonConvert.DeserializeObject<DailyRecordRoot>(jsonResponse);
				if (data?.TimeSeries == null)
				{
					throw new NetFinanceException($"no daily records for {symbol}");
				}
				foreach (var item in data.TimeSeries)
				{
					var today = item.Key;
					if (today > (endDate ?? startDate) || today < startDate || today > endDate)
					{
						continue;
					}
					if (result.Any(e => e.Date == today))
					{
						var errMsg = $"Bug: Course for {symbol} for {today:yyyy-MM-dd} already added!";
						_logger.LogWarning(errMsg);
					}
					else
					{
						result.Add(new DailyRecord
						{
							Date = today,
							Open = item.Value.Open,
							Low = item.Value.Low,
							High = item.Value.High,
							Close = item.Value.Close,
							AdjustedClose = item.Value.AdjustedClose,
							Volume = item.Value.Volume,
							SplitCoefficient = item.Value.SplitCoefficient,
						});
					}
				}
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogInformation($"Retry for {symbol}");
				lastException = ex;
				await Task.Delay(TimeSpan.FromSeconds(10), token);
			}
		}
		throw new NetFinanceException($"no daily records for {symbol} after {_options.Http_Retries} retries", lastException);
	}

	public async Task<IEnumerable<IntradayRecord>> GetIntradayRecordsAsync(string symbol, DateTime startDate, DateTime? endDate = null, EInterval interval = EInterval.Interval_15Min, CancellationToken token = default)
	{
		using var httpClient = _httpClientFactory.CreateClient(_options.AlphaVantage_Http_ClientName);
		Exception? lastException = new();
		Guard.Against.NullOrEmpty(symbol);
		var result = new List<IntradayRecord>();
		if (endDate == null || endDate?.Date >= DateTime.UtcNow.Date)
		{
			endDate = DateTime.UtcNow.Date;
		}
		if (startDate > endDate)
		{
			throw new NetFinanceException("Startdate after Endate");
		}

		for (var currentMonth = new DateTime(startDate.Year, startDate.Month, 1); currentMonth <= endDate; currentMonth = currentMonth.AddMonths(1))
		{
			if (currentMonth == endDate)
			{
				// dont query for data which not exists (api exception)
				if (endDate.Value.Day == 1 && endDate.Value.DayOfWeek == DayOfWeek.Saturday ||
					endDate.Value.Day == 1 && endDate.Value.DayOfWeek == DayOfWeek.Sunday ||
					endDate.Value.Day == 2 && endDate.Value.DayOfWeek == DayOfWeek.Sunday)
				{
					break;
				}
			}
			var currentCourses = await GetIntradayRecordsByMonthAsync(symbol, currentMonth, interval, token);
			result.AddRange(currentCourses);
		}
		return result.IsNullOrEmpty()
			? throw new NetFinanceException($"No intraday records found for {symbol}")
			: result.Where(e => e.DateTime >= startDate).ToList();
	}

	private async Task<List<IntradayRecord>> GetIntradayRecordsByMonthAsync(string symbol, DateTime month, EInterval interval, CancellationToken token = default)
	{
		using var httpClient = _httpClientFactory.CreateClient(_options.AlphaVantage_Http_ClientName);
		var result = new List<IntradayRecord>();
		Exception? lastException = new();

		for (int retryAttempt = 0; retryAttempt < _options.Http_Retries; retryAttempt++)
		{
			try
			{
				var requestUrl = _options.AlphaVantage_ApiUrl + "/query?function=TIME_SERIES_INTRADAY" +
					$"&symbol={symbol}" +
					$"&interval={interval.Description()}" +
					$"&month={month:yyyy-MM}" +
					"&extended_hours=false" +      // no pre and post market trading
					"&outputsize=full" +
					$"&apikey={_options.AlphaVantageApiKey}";

				var response = await httpClient.GetAsync(requestUrl, token);

				if (response.StatusCode == HttpStatusCode.NotFound)
				{
					throw new NetFinanceException($"No intraday records found for {symbol}");
				}
				response.EnsureSuccessStatusCode();

				string jsonResponse = await response.Content.ReadAsStringAsync();
				if (jsonResponse.Contains("higher API call volume"))
				{
					throw new NetFinanceException($"higher API call volume for {symbol}");
				}
				var data = JsonConvert.DeserializeObject<IntradayRecordRoot>(jsonResponse);

				var timeseries = interval switch
				{
					EInterval.Interval_1Min => data?.TimeSeries1Min,
					EInterval.Interval_5Min => data?.TimeSeries5Min,
					EInterval.Interval_15Min => data?.TimeSeries15Min,
					EInterval.Interval_30Min => data?.TimeSeries30Min,
					EInterval.Interval_60Min => data?.TimeSeries60Min,
					_ => throw new NotImplementedException(),
				};

				if (timeseries == null)
				{
					throw new NetFinanceException($"no intraday records for {symbol}");
				}
				foreach (var item in timeseries)
				{
					var dateTimeString = item.Key;
					var dateTimeSplit = dateTimeString.Split(' ');

					var date = dateTimeSplit[0];
					var time = dateTimeSplit[1];

					var dateTime = DateTime.ParseExact(dateTimeString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

					result.Add(new IntradayRecord
					{
						DateOnly = date,
						DateTime = dateTime,
						TimeOnly = time,

						Open = item.Value.Open,
						Low = item.Value.Low,
						High = item.Value.High,
						Close = item.Value.Close,
						Volume = item.Value.Volume,
					});
				}
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogInformation($"Retry for {symbol}");
				lastException = ex;
				await Task.Delay(TimeSpan.FromSeconds(10), token);
			}
		}
		throw new NetFinanceException($"No intraday records found for {symbol} after {_options.Http_Retries} retries", lastException);
	}

	public async Task<IEnumerable<DailyForexRecord>> GetDailyForexRecordsAsync(string currency1, string currency2, DateTime startDate, DateTime? endDate = null, CancellationToken token = default)
	{
		using var httpClient = _httpClientFactory.CreateClient(_options.AlphaVantage_Http_ClientName);
		Exception? lastException = new();
		Guard.Against.NullOrEmpty(currency1);
		Guard.Against.NullOrEmpty(currency2);
		var result = new List<DailyForexRecord>();

		if (endDate == null || endDate?.Date >= DateTime.UtcNow.Date)
		{
			endDate = DateTime.UtcNow.Date;
		}
		if (startDate > endDate)
		{
			throw new NetFinanceException("Startdate after Endate");
		}
		var daysToImport = ((endDate ?? startDate) - startDate).TotalDays;
		for (int retryAttempt = 0; retryAttempt < _options.Http_Retries; retryAttempt++)
		{
			try
			{
				var requestUrl = _options.AlphaVantage_ApiUrl + "/query?function=FX_DAILY" +
					$"&from_symbol={currency1}&to_symbol={currency2}&outputsize=full&apikey={_options.AlphaVantageApiKey}";

				var response = await httpClient.GetAsync(requestUrl, token);

				if (response.StatusCode == HttpStatusCode.NotFound)
				{
					throw new NetFinanceException($"No forex records found for {currency1} /{currency2}");
				}
				response.EnsureSuccessStatusCode();

				string jsonResponse = await response.Content.ReadAsStringAsync();
				if (jsonResponse.Contains("higher API call volume"))
				{
					throw new NetFinanceException($"higher API call volume for {currency1} /{currency2}");
				}

				var data = JsonConvert.DeserializeObject<DailyForexRecordRoot>(jsonResponse);
				if (data?.TimeSeries == null)
				{
					throw new NetFinanceException($"no forex records for {currency1} /{currency2}");
				}
				foreach (var item in data.TimeSeries)
				{
					var today = item.Key;
					if (today > (endDate ?? startDate) || today < startDate || today > endDate)
					{
						continue;
					}
					if (result.Any(e => e.Date == today))
					{
						var errMsg = $"Bug: {currency1} /{currency2} for {today:yyyy-MM-dd} already added!";
						_logger.LogWarning(errMsg);
					}
					else
					{
						result.Add(new DailyForexRecord
						{
							Date = item.Key,
							Open = item.Value.Open,
							Low = item.Value.Low,
							High = item.Value.High,
							Close = item.Value.Close,
						});
					}
				}
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogInformation($"Retry for {currency1} /{currency2}");
				lastException = ex;
				await Task.Delay(TimeSpan.FromSeconds(10), token);
			}
		}
		throw new NetFinanceException($"No forex records found for {currency1} /{currency2} after {_options.Http_Retries} retries", lastException);
	}
}

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetFinance.Exceptions;
using NetFinance.Extensions;
using NetFinance.Interfaces;
using NetFinance.Services;
using NUnit.Framework;

namespace NetFinance.Tests.IntegrationTests;

[TestFixture]
[Category("IntegrationTests")]
public class AlphaVantageTests
{
	private IServiceProvider _serviceProvider;
	private IAlphaVantageService _service;

	[SetUp]
	public void OneTimeSetUp()
	{
		var serviceProvider = new ServiceCollection()
			.AddLogging(builder =>
			{
				builder.AddConsole();
				builder.SetMinimumLevel(LogLevel.Information);
			});
		var builder = new ConfigurationBuilder();
		builder.AddUserSecrets<OpenDataTests>();
		var configuration = builder.Build();

		var services = new ServiceCollection();
		services.AddLogging(builder =>
		{
			builder.AddConsole();
			builder.SetMinimumLevel(LogLevel.Information);
		});
		services.AddSingleton<IConfiguration>(configuration);


		services.AddNetFinance(new NetFinanceConfiguration
		{
			AlphaVantageApiKey = configuration["NetFinanceConfiguration:AlphavantageApiKey"],
			Http_Timeout = 10,
			Http_Retries = 3
		});

		_serviceProvider = services.BuildServiceProvider();
		_service = _serviceProvider.GetRequiredService<IAlphaVantageService>();
		var logger = _serviceProvider.GetRequiredService<ILogger<AlphaVantageTests>>();

		var config1 = configuration["NetFinanceConfiguration:AlphavantageApiKey"];
		var config2 = configuration["NetFinanceConfiguration__AlphaVantageApiKey"];
		var config3 = configuration["NET_FINANCE_CONFIGURATION__ALPHAVANTAGE_API_KEY"];
		var config4 = configuration["NETFINANCECONFIGURATION__ALPHAVANTAGEAPIKEY"];
		var config5 = configuration["NETFINANCECONFIGURATIONALPHAVANTAGEAPIKEY"];
		logger.LogInformation($"1 NetFinanceConfiguration:AlphavantageApiKey={config1?.Length}");
		logger.LogWarning($"2 NetFinanceConfiguration__AlphaVantageApiKey={config2?.Length}");
		logger.LogError($"3 NET_FINANCE_CONFIGURATION__ALPHAVANTAGE_API_KEY={config3?.Length}");
		logger.LogError($"4 NET_FINANCE_CONFIGURATION__ALPHAVANTAGE_API_KEY={config4?.Length}");
		logger.LogError($"5 NET_FINANCE_CONFIGURATION__ALPHAVANTAGE_API_KEY={config5?.Length}");

		throw new NetFinanceException($"1={config1?.Length},2={config2?.Length},3={config3?.Length},4={config4?.Length},5={config5?.Length}");

	}

	[Test]
	public async Task GetCompanyOverviewAsync_WithoutIoC_ValidSymbols_ReturnsOverview()
	{
		var cfg = _serviceProvider.GetRequiredService<IOptions<NetFinanceConfiguration>>();
		var configuration = _serviceProvider.GetService<IConfiguration>();

		var service = AlphaVantageService.Create(new NetFinanceConfiguration
		{
			AlphaVantageApiKey = cfg.Value.AlphaVantageApiKey
		});

		var overview = await service.GetCompanyInfoAsync("SAP");

		Assert.That(overview, Is.Not.Null);
		Assert.That(overview.Symbol, Is.EqualTo("SAP"));
	}

	[TestCase("MSFT")]      // Microsoft Corporation (Nasdaq)
	[TestCase("SAP")]       // SAP SE (Nasdaq)
	[TestCase("GOOG")]      // Alphabet (Nasdaq)
	public async Task GetCompanyOverviewAsync_ValidSymbols_ReturnsOverview(string symbol)
	{

		var overview = await _service.GetCompanyInfoAsync(symbol);

		Assert.That(overview, Is.Not.Null);
		Assert.That(overview.Symbol, Is.EqualTo(symbol));
	}

	[TestCase("MSFT")]      // Microsoft Corporation (Nasdaq)
	[TestCase("SAP")]       // SAP SE (Nasdaq)
	[TestCase("SAP.DE")]    // SAP SE (Xetra)
	[TestCase("VOO")]       // Vanguard S&P 500 ETF
	public async Task GetDailyRecordsAsync_ValidSymbols_ReturnsRecords(string symbol)
	{
		var records = await _service.GetDailyRecordsAsync(symbol, DateTime.UtcNow.AddDays(-7));

		Assert.That(records, Is.Not.Empty);
	}

	[TestCase("MSFT")]      // Microsoft Corporation (Nasdaq)
	[TestCase("SAP")]       // SAP SE (Nasdaq)
	public async Task GetIntradayRecordsAsync_ValidSymbols_ReturnsRecords(string symbol)
	{
		var startDay = new DateTime(2024, 12, 02);
		var endDay = new DateTime(2024, 12, 02);
		var records = await _service.GetIntradayRecordsAsync(symbol, startDay, endDay);

		Assert.That(records, Is.Not.Empty);
	}

	[TestCase("EUR", "USD")]
	public async Task GetDailyForexRecordsAsync_ValidCurrencies_ReturnsRecords(string currency1, string currency2)
	{
		var records = await _service.GetDailyForexRecordsAsync(currency1, currency2, DateTime.UtcNow.AddDays(-3));

		Assert.That(records, Is.Not.Empty);
	}
}

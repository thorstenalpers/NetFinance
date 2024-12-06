using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
		var builder = new ConfigurationBuilder();
		builder.AddEnvironmentVariables();
		builder.AddUserSecrets<AlphaVantageTests>();
		var configuration = builder.Build();
		var services = new ServiceCollection();
		services.AddSingleton<IConfiguration>(configuration);

		Console.WriteLine("YYYYYYYY1 " + configuration["NET_FINANCE_CONFIGURATION__ALPHAVANTAGE_API_KEY"]);
		Console.WriteLine("YYYYYYYY2 " + configuration["FOO"]);


		services.AddNetFinance(
			new NetFinanceConfiguration
			{
				AlphaVantageApiKey = configuration["NetFinanceConfiguration:AlphavantageApiKey"],
				Http_Retries = 2,
				Http_Timeout = 5
			});

		_serviceProvider = services.BuildServiceProvider();
		_service = _serviceProvider.GetRequiredService<IAlphaVantageService>();
	}

	[Test]
	public async Task GetCompanyOverviewAsync_WithoutIoC_ValidSymbols_ReturnsOverview()
	{
		var cfg = _serviceProvider.GetRequiredService<IOptions<NetFinanceConfiguration>>();
		var configuration = _serviceProvider.GetService<IConfiguration>();

		Console.WriteLine("XXXXXXXX1 " + cfg.Value.AlphaVantageApiKey);
		Console.WriteLine("XXXXXXXX2 " + configuration["NET_FINANCE_CONFIGURATION__ALPHAVANTAGE_API_KEY"]);
		Console.WriteLine("XXXXXXXX3 " + configuration["FOO"]);

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

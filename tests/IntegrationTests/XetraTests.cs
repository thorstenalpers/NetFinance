using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetFinance.Extensions;
using NetFinance.Interfaces;
using NetFinance.Services;
using NUnit.Framework;

namespace NetFinance.Tests.IntegrationTests;

[TestFixture]
[Category("IntegrationTests")]
public class XetraTests
{
	private static IServiceProvider _serviceProvider;
	private IXetraService _service;

	[OneTimeSetUp]
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
		builder.AddEnvironmentVariables();
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
			Http_Timeout = 10,
			Http_Retries = 3
		});

		_serviceProvider = services.BuildServiceProvider();
	}

	[SetUp]
	public void Setup()
	{
		_service = _serviceProvider.GetRequiredService<IXetraService>();
	}

	[TestCase("MSF.DE")]    // Microsoft Corporation (Xetra)
	[TestCase("SAP.DE")]    // SAP SE (Xetra)
	[TestCase("VUSA.DE")]   // Vanguard S&P 500 ETF
	public async Task GetTradableInstruments_ValidSymbols_ReturnsIntsruments(string symbol)
	{
		var instruments = await _service.GetInstruments();

		Assert.That(instruments, Is.Not.Empty);

		var instrument = instruments.FirstOrDefault(e => e.Symbol == symbol);

		Assert.That(instrument, Is.Not.Null);
		Assert.That(instrument?.ISIN, Is.Not.Empty);
		Assert.That(instrument?.InstrumentName, Is.Not.Empty);
	}

	[Test]
	public async Task GetTradableInstruments_WithoutIoC_ReturnsInstruments()
	{
		var service = XetraService.Create();
		var instruments = await service.GetInstruments();

		Assert.That(instruments, Is.Not.Empty);
	}
}

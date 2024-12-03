using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NetFinance.Application.Extensions;
using NetFinance.Application.Services;
using NUnit.Framework;

namespace NetFinance.Tests;

[TestFixture]
[Category("IntegrationTests")]
public class IntegrationTests
{
	private INetFinanceService _dotNetFinanceService;

	[SetUp]
	public void Setup()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddNetFinance();
		var service = serviceCollection.BuildServiceProvider();
		_dotNetFinanceService = service.GetRequiredService<INetFinanceService>();
	}

	[TestCase("MSFT")]
	[TestCase("SAP")]
	[TestCase("SAP.DE")]
	[TestCase("6758.T")]
	public async Task GetSecurityAsync_Success(string symbol)
	{
		var security = await _dotNetFinanceService.GetSecurityAsync(symbol);

		Assert.That(security, Is.Not.Null);
		Assert.That(security.Symbol, Is.EqualTo(symbol));
	}

	[TestCase("MSFT")]
	[TestCase("SAP")]
	[TestCase("SAP.DE")]
	[TestCase("6758.T")]
	public async Task GetRecordsAsync_Success(string symbol)
	{
		var startDate = DateTime.UtcNow.AddDays(-7);
		var records = await _dotNetFinanceService.GetRecordsAsync(symbol, startDate);

		Assert.That(records, Is.Not.Empty);

		var lastRecentRecord = records.FirstOrDefault();
		Assert.That(lastRecentRecord.Date.Date <= DateTime.UtcNow, Is.True);
		Assert.That(lastRecentRecord.Date.Date >= startDate, Is.True);
	}

	[Test]
	public async Task GetRecordsAsync_WithDividend_Success()
	{
		var startDate = new DateTime(2020, 01, 01);
		var records = await _dotNetFinanceService.GetRecordsAsync("SAP.DE", startDate);

		Assert.That(records, Is.Not.Empty);

		var lastRecentRecord = records.FirstOrDefault();
		Assert.That(lastRecentRecord.Date.Date <= DateTime.UtcNow, Is.True);
		Assert.That(lastRecentRecord.Date.Date >= startDate, Is.True);
	}

	[Test]
	public async Task GetRecordsAsync_SapCoursesExactMatch_Success()
	{
		var startDate = new DateTime(2024, 01, 04);
		var endDate = new DateTime(2024, 01, 05);
		var records = await _dotNetFinanceService.GetRecordsAsync("SAP.DE", startDate, endDate);

		Assert.That(records, Is.Not.Empty);
		Assert.That(records.Count(), Is.EqualTo(2));

		var record1 = records.FirstOrDefault();
		var record2 = records.Skip(1).FirstOrDefault();

		Assert.That(record1.Date.Date, Is.EqualTo(new DateTime(2024, 01, 05).Date));
		Assert.That(record1.Open, Is.EqualTo(134.82m));
		Assert.That(record1.High, Is.EqualTo(137.58m));
		Assert.That(record1.Low, Is.EqualTo(134.42m));
		Assert.That(record1.Close, Is.EqualTo(137.08m));
		Assert.That(record1.AdjustedClose, Is.EqualTo(135.37m));
		Assert.That(record1.Volume, Is.EqualTo(1171604));

		Assert.That(record2.Date.Date, Is.EqualTo(new DateTime(2024, 01, 04).Date));
		Assert.That(record2.Open, Is.EqualTo(136.92m));
		Assert.That(record2.High, Is.EqualTo(137.76m));
		Assert.That(record2.Low, Is.EqualTo(136.18m));
		Assert.That(record2.Close, Is.EqualTo(136.44m));
		Assert.That(record2.AdjustedClose, Is.EqualTo(134.74m));
		Assert.That(record2.Volume, Is.EqualTo(1114133));
	}
}

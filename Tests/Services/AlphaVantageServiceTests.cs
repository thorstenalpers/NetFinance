using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NetFinance.Interfaces;
using NetFinance.Services;
using NUnit.Framework;

namespace NetFinance.Tests.Services;

[TestFixture]
[Category("UnitTests")]
public class AlphaVantageServiceTests
{
	private Mock<ILogger<IAlphaVantageService>> _mockLogger;
	private Mock<IHttpClientFactory> _mockHttpClientFactory;
	private Mock<HttpMessageHandler> _mockHandler;
	private Mock<IOptions<NetFinanceConfiguration>> _mockOptions;

	[SetUp]
	public void SetUp()
	{
		_mockOptions = new Mock<IOptions<NetFinanceConfiguration>>();
		_mockOptions.Setup(x => x.Value).Returns(new NetFinanceConfiguration
		{
			Http_Retries = 1
		});
		_mockLogger = new Mock<ILogger<IAlphaVantageService>>();
		_mockHttpClientFactory = new Mock<IHttpClientFactory>();
		_mockHandler = new Mock<HttpMessageHandler>();

		_mockHandler
			.Protected()
			.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") });
		_mockHttpClientFactory.Setup(e => e.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHandler.Object));
	}

	[Test]
	public void Create_Static_ReturnsObject()
	{
		// Arrange
		NetFinanceConfiguration cfg = null;

		// Act
		var service = AlphaVantageService.Create(cfg);

		// Assert
		Assert.That(service, Is.Not.Null);
	}

	[Test]
	public async Task GetCompanyOverviewAsync_OfIbm_ReturnsResult()
	{
		var filePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "AlphaVantage", "companyOverview.json");
		var jsonContent = File.ReadAllText(filePath);
		_mockHandler
			.Protected()
			.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(jsonContent, Encoding.UTF8, "application/json") });
		_mockHttpClientFactory.Setup(e => e.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHandler.Object));

		var service = new AlphaVantageService(
			_mockLogger.Object,
			_mockHttpClientFactory.Object,
			_mockOptions.Object);
		var symbol = "IBM";

		// Act
		var result = await service.GetCompanyInfoAsync(symbol);

		// Assert
		Assert.That(result, Is.Not.Null);
		Assert.That(result.Symbol, Is.EqualTo(symbol));
	}

	[Test]
	public async Task GetDailyRecordsAsync_OfIbm_ReturnsResult()
	{
		var filePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "AlphaVantage", "record.json");
		SetupHttpJsonFileResponse(filePath);

		var service = new AlphaVantageService(
			_mockLogger.Object,
			_mockHttpClientFactory.Object,
			_mockOptions.Object);

		var symbol = "IBM";
		var startDate = new DateTime(2024, 01, 01);
		DateTime? endDate = null;

		// Act
		var result = await service.GetDailyRecordsAsync(
			symbol,
			startDate,
			endDate);

		// Assert
		Assert.That(result, Is.Not.Empty);
		Assert.That(result.All(e => e.Open != null));
		Assert.That(result.All(e => e.Low != null));
		Assert.That(result.All(e => e.High != null));
		Assert.That(result.All(e => e.Close != null));
		Assert.That(result.All(e => e.Volume != null));
		Assert.That(result.All(e => e.AdjustedClose != null));
	}

	[Test]
	public async Task GetIntradayRecordsAsync_OfIbm_ReturnsResult()
	{
		var filePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "AlphaVantage", "intradayRecord.json");
		SetupHttpJsonFileResponse(filePath);

		var service = new AlphaVantageService(
			_mockLogger.Object,
			_mockHttpClientFactory.Object,
			_mockOptions.Object);

		var symbol = "IBM";
		var startDate = new DateTime(2024, 01, 01);
		DateTime? endDate = null;
		var interval = Models.AlphaVantage.EInterval.Interval_5Min;

		// Act
		var result = await service.GetIntradayRecordsAsync(symbol, startDate, endDate, interval);

		// Assert
		Assert.That(result, Is.Not.Empty);
		Assert.That(result.All(e => !string.IsNullOrWhiteSpace(e.DateOnly)));
		Assert.That(result.All(e => !string.IsNullOrWhiteSpace(e.TimeOnly)));
	}

	[Test]
	public async Task GetDailyForexRecordsAsync_WithEurUsd_ReturnsResult()
	{
		var filePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "AlphaVantage", "forex.json");
		SetupHttpJsonFileResponse(filePath);

		var service = new AlphaVantageService(
			_mockLogger.Object,
			_mockHttpClientFactory.Object,
			_mockOptions.Object);

		var currency1 = "EUR";
		var currency2 = "USD";
		var startDate = new DateTime(2024, 11, 01);
		DateTime? endDate = null;

		// Act
		var result = await service.GetDailyForexRecordsAsync(
			currency1,
			currency2,
			startDate,
			endDate);

		// Assert
		Assert.That(result, Is.Not.Empty);
		Assert.That(result.All(e => e.Open != null));
		Assert.That(result.All(e => e.Date != null));
		Assert.That(result.All(e => e.Low != null));
		Assert.That(result.All(e => e.High != null));
		Assert.That(result.All(e => e.Close != null));
	}

	private void SetupHttpJsonFileResponse(string filePath)
	{
		var jsonContent = File.ReadAllText(filePath);
		_mockHandler
			.Protected()
			.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(jsonContent, Encoding.UTF8, "application/json") });
		_mockHttpClientFactory.Setup(e => e.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHandler.Object));
	}
}

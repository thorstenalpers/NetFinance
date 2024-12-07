using System;
using System.Collections.Generic;
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
public class YahooServiceTests
{
	private Mock<ILogger<IYahooService>> _mockLogger;
	private Mock<IHttpClientFactory> _mockHttpClientFactory;
	private Mock<IYahooSession> _mockYahooSession;
	private Mock<IOptions<NetFinanceConfiguration>> _mockOptions;
	private Mock<HttpMessageHandler> _mockHandler;

	[SetUp]
	public void SetUp()
	{
		_mockOptions = new Mock<IOptions<NetFinanceConfiguration>>();
		_mockOptions.Setup(x => x.Value).Returns(new NetFinanceConfiguration { });
		_mockLogger = new Mock<ILogger<IYahooService>>();
		_mockHttpClientFactory = new Mock<IHttpClientFactory>();
		_mockHandler = new Mock<HttpMessageHandler>();
		_mockYahooSession = new Mock<IYahooSession>();

		_mockHandler
			.Protected()
			.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("", Encoding.UTF8, "text/html"),
				Headers =
				{
					{ "Set-Cookie", "\"A3=d=AQABBIPiUmcCEKLS0S2dxFEvSY2wq0BTJc4FEgEBAQE0VGdcZ-AMyiMA_eMAAA&S=AQAAAueeOka9YBgG-7Z2662G2t0; Expires=Mo, 10 Dec 2040 17:39:47 GMT; Max-Age=99931557600; Domain=.yahoo.com; Path=/; SameSite=None; Secure; HttpOnly\"" } // Add custom headers here
                }
			});
		_mockHttpClientFactory.Setup(e => e.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHandler.Object));
	}

	[Test]
	public void Create_Static_ReturnsObject()
	{
		// Arrange
		NetFinanceConfiguration cfg = null;

		// Act
		var service = YahooService.Create(cfg);

		// Assert
		Assert.That(service, Is.Not.Null);
	}

	[Test]
	public async Task GetQuoteAsync_OfIbm_ReturnsResult()
	{
		// Arrange
		var filePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "Yahoo", "quote.json");
		SetupHttpJsonFileResponse(filePath);

		var service = new YahooService(
			_mockLogger.Object,
			_mockHttpClientFactory.Object,
			_mockYahooSession.Object,
			_mockOptions.Object);

		var symbol = "IBM";

		// Act
		var result = await service.GetQuoteAsync(symbol);

		// Assert
		Assert.That(result, Is.Not.Null);
		Assert.That(result.Symbol, Is.EqualTo(symbol));
		Assert.That(!string.IsNullOrWhiteSpace(result.ShortName));
		Assert.That(result.MarketCap > 0);
	}

	[Test]
	public async Task GetQuotesAsync_OfIbm_ReturnsResult()
	{
		// Arrange
		var filePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "Yahoo", "quote.json");
		SetupHttpJsonFileResponse(filePath);

		var service = new YahooService(
			_mockLogger.Object,
			_mockHttpClientFactory.Object,
			_mockYahooSession.Object,
			_mockOptions.Object);

		var symbols = new List<string> { "IBM" };

		// Act
		var result = await service.GetQuotesAsync(symbols);

		// Assert
		Assert.That(result, Is.Not.Empty);
		var first = result.FirstOrDefault();
		Assert.That(first.Symbol, Is.EqualTo(symbols.FirstOrDefault()));
		Assert.That(!string.IsNullOrWhiteSpace(first.ShortName));
		Assert.That(first.MarketCap > 0);
	}

	[Test]
	public async Task GetProfileAsync_OfIbm_ReturnsResult()
	{
		// Arrange
		var filePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "Yahoo", "profile.html");
		SetupHttpHtmlFileResponse(filePath);

		var service = new YahooService(
			_mockLogger.Object,
			_mockHttpClientFactory.Object,
			_mockYahooSession.Object,
			_mockOptions.Object);

		var symbol = "IBM";

		// Act
		var result = await service.GetProfileAsync(symbol);

		// Assert
		Assert.That(result, Is.Not.Null);
		Assert.That(!string.IsNullOrWhiteSpace(result.Phone));
		Assert.That(!string.IsNullOrWhiteSpace(result.Description));
	}

	[Test]
	public async Task GetRecordsAsync_OfIbm_ReturnsResult()
	{
		// Arrange
		var filePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "Yahoo", "records.html");
		SetupHttpHtmlFileResponse(filePath);
		var service = new YahooService(
			_mockLogger.Object,
			_mockHttpClientFactory.Object,
			_mockYahooSession.Object,
			_mockOptions.Object);

		var symbol = "IBM";
		DateTime startDate = default;
		DateTime? endDate = null;

		// Act
		var result = await service.GetDailyRecordsAsync(symbol, startDate, endDate);

		// Assert
		Assert.That(result, Is.Not.Empty);
		Assert.That(result.All(e => e.Open != null));
		Assert.That(result.All(e => e.Close != null));
		Assert.That(result.All(e => e.AdjustedClose != null));
		Assert.That(result.All(e => e.Low != null));
		Assert.That(result.All(e => e.High != null));
	}

	[Test]
	public async Task GetFinancialReportsAsync_OfIbm_ReturnsResult()
	{
		// Arrange
		var filePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "Yahoo", "financial.html");
		SetupHttpHtmlFileResponse(filePath);

		var service = new YahooService(
			_mockLogger.Object,
			_mockHttpClientFactory.Object,
			_mockYahooSession.Object,
			_mockOptions.Object);

		var symbol = "IBM";

		// Act
		var result = await service.GetFinancialReportsAsync(symbol);

		// Assert
		Assert.That(result, Is.Not.Empty);
		var first = result.First();
		Assert.That(!string.IsNullOrWhiteSpace(first.Key));
		Assert.That(first.Value.TotalRevenue != null);
		Assert.That(first.Value.BasicAverageShares != null);
		Assert.That(first.Value.EBIT != null);
	}

	[Test]
	public async Task GetSummaryAsync_OfIbm_ReturnsResult()
	{
		// Arrange		
		var filePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "Yahoo", "summary.html");
		SetupHttpHtmlFileResponse(filePath);

		var service = new YahooService(
			_mockLogger.Object,
			_mockHttpClientFactory.Object,
			_mockYahooSession.Object,
			_mockOptions.Object);

		var symbol = "IBM";

		// Act
		var result = await service.GetSummaryAsync(symbol);

		// Assert
		Assert.That(result, Is.Not.Null);
		Assert.That(result.PreviousClose != null);
		Assert.That(result.Ask != null);
		Assert.That(result.Bid != null);
		Assert.That(result.MarketCap_Intraday != null);
	}

	private void SetupHttpHtmlFileResponse(string filePath)
	{
		var jsonContent = File.ReadAllText(filePath);
		_mockHandler
			.Protected()
			.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(jsonContent, Encoding.UTF8, "text/html"),
				Headers =
				{
					{ "Set-Cookie", "\"A3=d=AQABBIPiUmcCEKLS0S2dxFEvSY2wq0BTJc4FEgEBAQE0VGdcZ-AMyiMA_eMAAA&S=AQAAAueeOka9YBgG-7Z2662G2t0; Expires=Mo, 10 Dec 2040 17:39:47 GMT; Max-Age=99931557600; Domain=.yahoo.com; Path=/; SameSite=None; Secure; HttpOnly\"" } // Add custom headers here
                }
			});
		_mockHttpClientFactory.Setup(e => e.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHandler.Object));
	}

	private void SetupHttpJsonFileResponse(string filePath)
	{
		var jsonContent = File.ReadAllText(filePath);
		_mockHandler
			.Protected()
			.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(jsonContent, Encoding.UTF8, "text/html"),
				Headers =
				{
					{ "Set-Cookie", "\"A3=d=AQABBIPiUmcCEKLS0S2dxFEvSY2wq0BTJc4FEgEBAQE0VGdcZ-AMyiMA_eMAAA&S=AQAAAueeOka9YBgG-7Z2662G2t0; Expires=Mo, 10 Dec 2040 17:39:47 GMT; Max-Age=99931557600; Domain=.yahoo.com; Path=/; SameSite=None; Secure; HttpOnly\"" } // Add custom headers here
                }
			});
		_mockHttpClientFactory.Setup(e => e.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHandler.Object));
	}
}

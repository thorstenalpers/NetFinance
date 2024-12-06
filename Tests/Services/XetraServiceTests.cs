using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NetFinance.Services;
using NUnit.Framework;

namespace NetFinance.Tests.Services;

[TestFixture]
[Category("UnitTests")]
public class XetraServiceTests
{
	private Mock<IHttpClientFactory> _mockHttpClientFactory;
	private Mock<IOptions<NetFinanceConfiguration>> _mockOptions;
	private Mock<HttpMessageHandler> _mockHandler;

	[SetUp]
	public void SetUp()
	{
		_mockOptions = new Mock<IOptions<NetFinanceConfiguration>>();
		_mockOptions.Setup(x => x.Value).Returns(new NetFinanceConfiguration { });
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
		var service = XetraService.Create(cfg);

		// Assert
		Assert.That(service, Is.Not.Null);
	}

	[Test]
	public async Task GetTradableInstruments_WithValidEntries_ReturnsResult()
	{
		// Arrange
		var filePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "Xetra", "t7-xetr-allTradableInstruments.csv");
		SetupHttpCsvFileResponse(filePath);
		var service = new XetraService(
			_mockHttpClientFactory.Object,
			_mockOptions.Object);

		// Act
		var result = await service.GetInstruments();

		// Assert

		Assert.That(result, Is.Not.Empty);
		Assert.That(result.All(e => !string.IsNullOrWhiteSpace(e.ISIN)));
		Assert.That(result.All(e => !string.IsNullOrWhiteSpace(e.Mnemonic)));
	}

	private void SetupHttpCsvFileResponse(string filePath)
	{
		var jsonContent = File.ReadAllText(filePath);
		_mockHandler
			.Protected()
			.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(jsonContent, Encoding.UTF8, "text/csv") });
		_mockHttpClientFactory.Setup(e => e.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHandler.Object));
	}
}

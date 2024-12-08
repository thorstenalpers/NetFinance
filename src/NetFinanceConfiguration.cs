using System.ComponentModel.DataAnnotations;

namespace NetFinance;
public class NetFinanceConfiguration
{
	/// <summary> Alpha Vantage API Key </summary>
	public string? AlphaVantageApiKey { get; set; } = null;

	/// <summary> Default retries for failed http requests (caused by rate limits) </summary>
	[Required] public int Http_Retries = 3;

	/// <summary> Default HTTP timeout in seconds </summary>
	[Required] public int Http_Timeout = 30;

	/// <summary> time in hours after refrshing cookies </summary>
	[Required] public int Yahoo_Cookie_RefreshTime = 6;

	/// <summary> Base url for Yahoo UI content </summary>
	[Required] public string Yahoo_BaseUrl_Html = "https://finance.yahoo.com/quote";

	/// <summary> Base url to get cookie for api calls </summary>
	[Required] public string Yahoo_BaseUrl_Authentication = "https://fc.yahoo.com";

	/// <summary> Base url to get cookie for html calls </summary>
	[Required] public string Yahoo_BaseUrl_Consent = "https://guce.yahoo.com/consent";

	/// <summary> Base url to send consent cookie for html calls </summary>
	[Required] public string Yahoo_BaseUrl_Consent_Collect = "https://consent.yahoo.com/v2/collectConsent";

	/// <summary> Base url for Yahoo crumb API calls </summary>
	[Required] public string Yahoo_BaseUrl_Crumb_Api = "https://query1.finance.yahoo.com/v1/test/getcrumb";

	/// <summary> Base url for Yahoo quote API calls </summary>
	[Required] public string Yahoo_BaseUrl_Quote_Api = "https://query1.finance.yahoo.com/v7/finance/quote";

	/// <summary> Base url for Yahoo quote API calls </summary>
	[Required] public string Xetra_DownloadUrl_Instruments = "https://www.xetra.com/resource/blob/1528/76087c675c856fe7720917da03a62a34/data/t7-xetr-allTradableInstruments.csv";

	/// <summary> Download URL of OpenData S&P500 listed symbols </summary>
	[Required] public string OpenData_DownloadUrl_SP500Symbols = "https://raw.githubusercontent.com/datasets/s-and-p-500-companies-financials/refs/heads/main/data/constituents-financials.csv";

	/// <summary> Download URL of OpenData nasdaq listed symbols </summary>
	[Required] public string OpenData_DownloadUrl_NasdaqListedSymbols = "https://raw.githubusercontent.com/datasets/nasdaq-listings/refs/heads/main/data/nasdaq-listed-symbols.csv";

	/// <summary> Base url for Alpha Vantage API calls </summary>
	[Required] public string AlphaVantage_ApiUrl = "https://www.alphavantage.co";

	/// <summary> Name of the Yahoo HttpClient used from HttpClientFactory </summary>
	internal readonly string Yahoo_Http_ClientName = "NetFinanceYahooClient";

	/// <summary> Name of the Xetra HttpClient used from HttpClientFactory </summary>
	internal readonly string Xetra_Http_ClientName = "NetFinanceXetraClient";

	/// <summary> Name of the AlphaVantage HttpClient used from HttpClientFactory </summary>
	internal readonly string AlphaVantage_Http_ClientName = "NetFinanceAlphaVantageClient";

	/// <summary> Name of the Yahoo HttpClient used from HttpClientFactory </summary>
	internal readonly string OpenData_Http_ClientName = "NetFinanceOpenDataClient";
}
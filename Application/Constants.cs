namespace NetFinance.Application;

internal class Constants
{
	public static readonly string ApiClientName = "DotNetYahooClient";

	public static readonly string BaseUrl_Html = "https://finance.yahoo.com/quote";

	/// <summary>
	/// Base url for auth API calls
	/// </summary>
	public static readonly string BaseUrl_Auth_Api = "https://fc.yahoo.com";

	/// <summary>
	/// Base url for crumb API calls
	/// </summary>
	public static readonly string BaseUrl_Crumb_Api = "https://query1.finance.yahoo.com/v1/test/getcrumb";

	/// <summary>
	/// Base url for quote API calls
	/// </summary>
	public static readonly string BaseUrl_Quote_Api = "https://query1.finance.yahoo.com/v7/finance/quote";

	/// <summary>
	/// Defaul timeout in seconds
	/// </summary>
	public static readonly int Timeout = 30;
}

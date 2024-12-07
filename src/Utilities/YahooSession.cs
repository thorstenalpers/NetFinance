using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetFinance.Exceptions;
using NetFinance.Interfaces;

namespace NetFinance.Utilities;

internal class YahooSession(IOptions<NetFinanceConfiguration> options, ILogger<IYahooSession> logger) : IYahooSession
{
	private readonly ILogger<IYahooSession> _logger = logger;
	private SemaphoreSlim _semaphore = new(1, 1);
	private readonly NetFinanceConfiguration _options = options.Value ?? throw new ArgumentNullException(nameof(options));
	private CookieContainer? _cookieContainer;
	private string? _crumb;
	private DateTime? _refreshTime;

	public async Task RefreshSessionAsync(CancellationToken token = default, bool forceRefresh = false)
	{
		if (!forceRefresh && !string.IsNullOrEmpty(_crumb) && AreCookiesValid())
		{
			return;
		}
		await _semaphore.WaitAsync(token).ConfigureAwait(false);
		if (!forceRefresh && !string.IsNullOrEmpty(_crumb) && AreCookiesValid())
		{
			return;
		}
		try
		{
			Exception? lastException = null;
			for (int attempt = 1; attempt <= _options.Http_Retries; attempt++)
			{
				try
				{
					var cookieContainer = new CookieContainer();
					var handler = new HttpClientHandler
					{
						CookieContainer = cookieContainer
					};
					using var httpClient = new HttpClient(handler);
					var userAgent = Helper.CreateRandomUserAgent();
					httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);

					// get index
					var response6 = await httpClient.GetAsync("https://finance.yahoo.com");
					response6.EnsureSuccessStatusCode();

					// get consent
					await Task.Delay(TimeSpan.FromSeconds(1));
					var requestMessage = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, _options.Yahoo_BaseUrl_Consent);

					var response = await httpClient.SendAsync(requestMessage);
					response.EnsureSuccessStatusCode();
					var htmlContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

					foreach (Cookie cookie in cookieContainer.GetCookies(new Uri("https://finance.yahoo.com")))
					{
						_logger.LogInformation($"yyy: {cookie.Name}: {cookie.Value}");
					}

					var document = new AngleSharp.Html.Parser.HtmlParser().ParseDocument(htmlContent);
					var csrfTokenNode = document.QuerySelector("input[name='csrfToken']");
					var sessionIdNode = document.QuerySelector("input[name='sessionId']");
					if (csrfTokenNode == null || sessionIdNode == null)
					{
						throw new NetFinanceException("Failed to retrieve csrfTokenNode and sessionIdNode.");
					}
					var csrfToken = csrfTokenNode.GetAttribute("value");
					var sessionId = sessionIdNode.GetAttribute("value");
					if (string.IsNullOrEmpty(csrfToken) || string.IsNullOrEmpty(sessionId))
					{
						throw new NetFinanceException("Failed to retrieve csrfToken and sessionId.");
					}
					await Task.Delay(TimeSpan.FromSeconds(1));

					// reject consent
					var postData = new List<KeyValuePair<string, string>>
					{
						new("csrfToken", csrfToken),
						new("sessionId", sessionId),
						new("originalDoneUrl", "https://finance.yahoo.com/"),
						new("namespace", "yahoo"),
						new("consentUUID", "default")
					};
					foreach (var value in new List<string> { "reject", "reject" })
					{
						postData.Add(new("reject", value));
					}
					var url1 = $"{_options.Yahoo_BaseUrl_Consent_Collect}?sessionId=" + sessionId;
					requestMessage = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, url1)
					{
						Content = new FormUrlEncodedContent(postData),
					};
					requestMessage.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
					requestMessage.Headers.Add("Referer", url1);
					requestMessage.Headers.Add("Origin", "https://consent.yahoo.com");
					requestMessage.Headers.Add("DNT", "1");
					requestMessage.Headers.Add("Sec-GPC", "1");
					requestMessage.Headers.Add("Connection", "keep-alive");

					response = await httpClient.SendAsync(requestMessage);
					response.EnsureSuccessStatusCode();
					await Task.Delay(TimeSpan.FromSeconds(1));

					// finalize
					var url2 = $"{_options.Yahoo_BaseUrl_Consent}?sessionId=" + sessionId;
					response = await httpClient.GetAsync(url2);
					response.EnsureSuccessStatusCode();


					foreach (Cookie cookie in cookieContainer.GetCookies(new Uri("https://finance.yahoo.com")))
					{
						_logger.LogInformation($"xxxx: {cookie.Name}: {cookie.Value}");
					}

					// get crumb: used to make quote api calls
					var crumbResponse = await httpClient.GetAsync(_options.Yahoo_BaseUrl_Crumb_Api).ConfigureAwait(false);
					_crumb = await crumbResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
					if (string.IsNullOrEmpty(_crumb) || _crumb.Contains("Too Many Requests"))
					{
						_logger.LogWarning($"Failed to retrieve Yahoo crumb. crumb={_crumb}");
					}
					await Task.Delay(TimeSpan.FromSeconds(1));

					_cookieContainer = cookieContainer;
					_refreshTime = DateTime.UtcNow;
					return;
				}
				catch (Exception ex)
				{
					_logger.LogInformation($"Retry after exception {ex}");
					await Task.Delay(TimeSpan.FromSeconds(_options.Http_Retries_Waittime), token);
					lastException = ex;
				}
			}
			_logger.LogError("Failed to authenticate, lastException=" + lastException + "\n");
		}
		finally
		{
			_semaphore.Release();
		}
	}

	public bool AreCookiesValid()
	{
		if (_cookieContainer == null || _refreshTime == null)
		{
			return false;
		}
		var cookies = _cookieContainer?.GetCookies(new Uri("https://finance.yahoo.com"));
		if (cookies?.Count == null || cookies.Count == 0)
		{
			return false;
		}
		if (DateTime.UtcNow >= _refreshTime?.AddHours(_options.Yahoo_Cookie_RefreshTime))   // e.g. 10:00 >= 12:00 (09:00+3) = false, 10:00 >= 04:00 (01:00+3) = true
		{
			return false;
		}
		foreach (Cookie cookie in cookies)
		{
			var expiryDate = cookie.Expires;
			if (expiryDate != default)
			{
				if (expiryDate < DateTime.Now)
				{
					return false;
				}
			}
		}
		return true;
	}

	public string GetCrumb()
	{
		return _crumb ?? "";
	}

	public CookieCollection GetCookieCollection()
	{
		return _cookieContainer?.GetCookies(new Uri("https://finance.yahoo.com")) ?? [];
	}
}

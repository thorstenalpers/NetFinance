using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetFinance.Exceptions;
using NetFinance.Interfaces;

namespace NetFinance.Utilities;

internal class YahooSession(ILogger<IYahooSession> logger, IOptions<NetFinanceConfiguration> options) : IYahooSession
{
	private readonly ILogger<IYahooSession> _logger = logger;
	private readonly NetFinanceConfiguration _options = options.Value ?? throw new ArgumentNullException(nameof(options));
	private SemaphoreSlim _semaphore = new(1, 1); private readonly string _userAgent = Helper.CreateRandomUserAgent();
	private CookieContainer? _apiCookieContainer;
	private CookieContainer? _uiCookieContainer;
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
					// ******************** get auth cookie ********************
					var handler = new HttpClientHandler
					{
						CookieContainer = new CookieContainer(),
						UseCookies = true
					};
					using (var httpClient = new HttpClient(handler))
					{
						httpClient.DefaultRequestHeaders.Add("User-Agent", _userAgent);
						httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml,application/json;q=0.9,*/*;q=0.8");
						httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");

						var response = await httpClient.GetAsync(_options.Yahoo_BaseUrl_Authentication.ToLower(), token).ConfigureAwait(false);

						var requestMessage = new HttpRequestMessage(HttpMethod.Get, _options.Yahoo_BaseUrl_Crumb_Api.ToLower());
						var cookies = GetCookies(response);
						requestMessage.AddCookiesToRequest(cookies);

						response = await httpClient.SendAsync(requestMessage, token).ConfigureAwait(false);
						_crumb = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
						if (string.IsNullOrEmpty(_crumb) || _crumb.Contains("Too Many Requests"))
						{
							throw new NetFinanceException("Failed to retrieve Yahoo crumb.");
						}
						cookies = GetCookies(response);
						_apiCookieContainer = new CookieContainer();
						_apiCookieContainer.Add(cookies);
						if (cookies?.Count < 3)
						{
							throw new NetFinanceException("failed to get api cookies.");
						}
					}

					// ******************** get consent cookie ********************
					handler = new HttpClientHandler
					{
						CookieContainer = new CookieContainer(),
						UseCookies = true
					};
					using (var httpClient = new HttpClient(handler))
					{
						httpClient.DefaultRequestHeaders.Add("User-Agent", _userAgent);
						httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml,application/json;q=0.9,*/*;q=0.8");
						httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");

						// get consent
						await Task.Delay(TimeSpan.FromSeconds(1));
						var response = await httpClient.GetAsync(_options.Yahoo_BaseUrl_Consent.ToLower(), token);
						response.EnsureSuccessStatusCode();

						var htmlContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
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
							new("originalDoneUrl", "https://finance.yahoo.com"),
							new("namespace", "yahoo"),
							new("consentUUID", "default")
						};
						foreach (var value in new List<string> { "reject", "reject" })
						{
							postData.Add(new("reject", value));
						}
						var url = $"{_options.Yahoo_BaseUrl_Consent_Collect}?sessionId={sessionId}";
						var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
						{
							Content = new FormUrlEncodedContent(postData),
						};
						requestMessage.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
						requestMessage.Headers.Add("Referer", url);
						requestMessage.Headers.Add("DNT", "1");
						requestMessage.Headers.Add("Sec-GPC", "1");
						requestMessage.Headers.Add("Connection", "keep-alive");

						response = await httpClient.SendAsync(requestMessage, token);
						response.EnsureSuccessStatusCode();
						await Task.Delay(TimeSpan.FromSeconds(1));

						// finalize
						response = await httpClient.GetAsync((string?)$"{_options.Yahoo_BaseUrl_Consent}?sessionId={sessionId}", token);
						response.EnsureSuccessStatusCode();
						if (handler.CookieContainer?.Count < 3)
						{
							throw new NetFinanceException("failed to get ui cookies.");
						}
						_uiCookieContainer = handler.CookieContainer;
						_refreshTime = DateTime.UtcNow;
						_logger.LogInformation($"Session established successfully");
						return;
					};
				}
				catch (Exception ex)
				{
					_logger.LogInformation($"Retry after exception=--\n{ex}\n---");
					await Task.Delay((int)Math.Pow(2, attempt) * 1000);
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

	private static CookieCollection? GetCookies(HttpResponseMessage response)
	{
		try
		{
			var cookieHeaders = response.Headers.GetValues("Set-Cookie").ToList();

			var cookieContainer = new CookieContainer();
			foreach (var cookieHeader in cookieHeaders)
			{
				var uri = new Uri("https://finance.yahoo.com");
				cookieContainer.SetCookies(uri, cookieHeader);
			}
			var cookies = cookieContainer.GetCookies(new Uri("https://finance.yahoo.com"));
			return cookies;
		}
		catch
		{
			return null;
		}
	}

	public bool AreCookiesValid()
	{
		if (_uiCookieContainer == null || _refreshTime == null)
		{
			return false;
		}
		var cookies = _uiCookieContainer?.GetCookies(new Uri("https://finance.yahoo.com"));
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
	public string GetUserAgent()
	{
		return _userAgent ?? "";
	}

	public async Task<CookieCollection> GetAndRefreshApiCookies(CancellationToken token = default)
	{
		await RefreshSessionAsync(token).ConfigureAwait(false);
		return _apiCookieContainer?.GetCookies(new Uri("https://finance.yahoo.com")) ?? [];
	}
	public async Task<CookieCollection> GetAndRefreshUiCookies(CancellationToken token = default)
	{
		await RefreshSessionAsync(token).ConfigureAwait(false);
		return _uiCookieContainer?.GetCookies(new Uri("https://finance.yahoo.com")) ?? [];
	}
}

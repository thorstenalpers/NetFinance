using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NetFinance.Exceptions;
using NetFinance.Interfaces;

namespace NetFinance.Utilities;

internal class YahooSession(IHttpClientFactory httpClientFactory, IOptions<NetFinanceConfiguration> options) : IYahooSession
{
	private string? _crumb;
	private readonly YahooCookie _yahooCookie = new();
	private SemaphoreSlim _semaphore = new(1, 1);
	private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
	private readonly NetFinanceConfiguration _netFinanceOptions = options.Value ?? throw new ArgumentNullException(nameof(options));

	public async Task<(string crumb, Cookie cookie)> GetSessionStateAsync(CancellationToken token = default)
	{
		if (!string.IsNullOrEmpty(_crumb) && _yahooCookie?.Cookie != null && _yahooCookie.IsValid())
		{
			return (_crumb, _yahooCookie.Cookie);
		}

		await _semaphore.WaitAsync(token).ConfigureAwait(false);
		try
		{
			var httpClient = _httpClientFactory.CreateClient(_netFinanceOptions.Yahoo_Http_ClientName);
			var response = await httpClient.GetAsync(_netFinanceOptions.Yahoo_BaseUrl_Auth_Api, token).ConfigureAwait(false);
			var cookieStr = response.Headers.GetValues("Set-Cookie").FirstOrDefault();

			if (_yahooCookie == null) throw new NetFinanceException("YahooCookie is null");
			_yahooCookie.Parse(cookieStr);

			if (_yahooCookie.Cookie == null || !_yahooCookie.IsValid())
			{
				throw new NetFinanceException("Failed to obtain Yahoo auth cookie.");
			}
			var requestMessage = new HttpRequestMessage(HttpMethod.Get, _netFinanceOptions.Yahoo_BaseUrl_Crumb_Api);
			requestMessage.Headers.Add("Cookie", _yahooCookie.ToString());
			var crumbResponse = await httpClient.SendAsync(requestMessage).ConfigureAwait(false);
			_crumb = await crumbResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

			if (string.IsNullOrEmpty(_crumb))
			{
				throw new NetFinanceException("Failed to retrieve Yahoo crumb.");
			}
			return (_crumb, _yahooCookie.Cookie);
		}
		finally
		{
			_semaphore.Release();
		}
	}
}

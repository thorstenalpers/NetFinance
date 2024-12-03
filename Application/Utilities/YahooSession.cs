using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NetFinance.Application.Exceptions;

namespace NetFinance.Application.Utilities;

internal class YahooSession(IHttpClientFactory httpClientFactory) : IYahooSession
{
	private string? _crumb;
	private readonly YahooCookie _yahooCookie = new();
	private SemaphoreSlim _semaphore = new(1, 1);
	private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

	public async Task<(string crumb, Cookie cookie)> GetSessionStateAsync(CancellationToken token = default)
	{
		if (!string.IsNullOrEmpty(_crumb) && _yahooCookie?.Cookie != null && _yahooCookie.IsValid())
		{
			return (_crumb, _yahooCookie.Cookie);
		}

		await _semaphore.WaitAsync(token).ConfigureAwait(false);
		try
		{
			var httpClient = _httpClientFactory.CreateClient(Constants.ApiClientName);
			var response = await httpClient.GetAsync(Constants.BaseUrl_Auth_Api, token).ConfigureAwait(false);
			var cookieStr = response.Headers.GetValues("Set-Cookie").FirstOrDefault();


			if (_yahooCookie == null) throw new NetFinanceException("YahooCookie is null");
			_yahooCookie.Parse(cookieStr);

			if (_yahooCookie.Cookie == null || !_yahooCookie.IsValid())
			{
				throw new NetFinanceException("Failed to obtain Yahoo auth cookie.");
			}
			var requestMessage = new HttpRequestMessage(HttpMethod.Get, Constants.BaseUrl_Crumb_Api);
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

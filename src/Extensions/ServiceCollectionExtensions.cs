using System;
using Microsoft.Extensions.DependencyInjection;
using NetFinance.Interfaces;
using NetFinance.Services;
using NetFinance.Utilities;

namespace NetFinance.Extensions;
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Configures .Net Finance Service
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <param name="configuration">Optional: Default values to configure .Net Finance. <see cref="NetFinanceConfiguration"/> ></param>
	public static void AddNetFinance(this IServiceCollection services, NetFinanceConfiguration? cfg = null)
	{
		cfg ??= new NetFinanceConfiguration();

		services.Configure<NetFinanceConfiguration>(opt =>
		{
			opt.Http_Retries = cfg.Http_Retries;
			opt.Http_Timeout = cfg.Http_Timeout;

			opt.AlphaVantageApiKey = cfg.AlphaVantageApiKey;
			opt.AlphaVantage_ApiUrl = cfg.AlphaVantage_ApiUrl;

			opt.Xetra_DownloadUrl_Instruments = cfg.Xetra_DownloadUrl_Instruments;

			opt.DatahubIo_DownloadUrl_SP500Symbols = cfg.DatahubIo_DownloadUrl_SP500Symbols;
			opt.DatahubIo_DownloadUrl_NasdaqListedSymbols = cfg.DatahubIo_DownloadUrl_NasdaqListedSymbols;

			opt.Yahoo_BaseUrl_Html = cfg.Yahoo_BaseUrl_Html;
			opt.Yahoo_BaseUrl_Authentication = cfg.Yahoo_BaseUrl_Authentication;
			opt.Yahoo_BaseUrl_Crumb_Api = cfg.Yahoo_BaseUrl_Crumb_Api;
			opt.Yahoo_BaseUrl_Quote_Api = cfg.Yahoo_BaseUrl_Quote_Api;
			opt.Yahoo_Cookie_RefreshTime = cfg.Yahoo_Cookie_RefreshTime;
			opt.Yahoo_BaseUrl_Consent = cfg.Yahoo_BaseUrl_Consent;
			opt.Yahoo_BaseUrl_Consent_Collect = cfg.Yahoo_BaseUrl_Consent_Collect;
		});

		services.AddSingleton<IYahooSession, YahooSession>();

		services.AddScoped<IYahooService, YahooService>();
		services.AddScoped<IXetraService, XetraService>();
		services.AddScoped<IAlphaVantageService, AlphaVantageService>();
		services.AddScoped<IDataHubIoService, DataHubIoService>();

		services.AddHttpClient(cfg.Yahoo_Http_ClientName)
			.ConfigureHttpClient((provider, client) =>
			{
				var session = provider.GetRequiredService<IYahooSession>();
				client.DefaultRequestHeaders.Add("User-Agent", session.GetUserAgent());
				client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
				client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
				client.Timeout = TimeSpan.FromSeconds(cfg.Http_Timeout);
			});

		services.AddHttpClient(cfg.Xetra_Http_ClientName)
			.ConfigureHttpClient(client =>
			{
				var userAgent = Helper.CreateRandomUserAgent();
				client.DefaultRequestHeaders.Add("User-Agent", userAgent);
				client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml,application/json;q=0.9,*/*;q=0.8");
				client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
				client.Timeout = TimeSpan.FromSeconds(cfg.Http_Timeout);
			});

		services.AddHttpClient(cfg.AlphaVantage_Http_ClientName)
			.ConfigureHttpClient(client =>
			{
				var userAgent = Helper.CreateRandomUserAgent();
				client.DefaultRequestHeaders.Add("User-Agent", userAgent);
				client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml,application/json;q=0.9,*/*;q=0.8");
				client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
				client.Timeout = TimeSpan.FromSeconds(cfg.Http_Timeout);
			});

		services.AddHttpClient(cfg.DatahubIo_Http_ClientName)
			.ConfigureHttpClient(client =>
			{
				var userAgent = Helper.CreateRandomUserAgent();
				client.DefaultRequestHeaders.Add("User-Agent", userAgent);
				client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml,application/json;q=0.9,*/*;q=0.8");
				client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
				client.Timeout = TimeSpan.FromSeconds(cfg.Http_Timeout);
			});
	}
}
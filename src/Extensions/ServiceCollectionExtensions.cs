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
			opt.AlphaVantageApiKey = cfg.AlphaVantageApiKey;
			opt.Http_Retries = cfg.Http_Retries;
			opt.Yahoo_BaseUrl_Html = cfg.Yahoo_BaseUrl_Html;
			opt.Yahoo_BaseUrl_Auth_Api = cfg.Yahoo_BaseUrl_Auth_Api;
			opt.Yahoo_BaseUrl_Crumb_Api = cfg.Yahoo_BaseUrl_Crumb_Api;
			opt.Yahoo_BaseUrl_Quote_Api = cfg.Yahoo_BaseUrl_Quote_Api;
			opt.Http_Timeout = cfg.Http_Timeout;
			opt.Xetra_DownloadUrl_Instruments = cfg.Xetra_DownloadUrl_Instruments;
			opt.OpenData_DownloadUrl_SP500Symbols = cfg.OpenData_DownloadUrl_SP500Symbols;
			opt.OpenData_DownloadUrl_NasdaqListedSymbols = cfg.OpenData_DownloadUrl_NasdaqListedSymbols;
			opt.AlphaVantage_ApiUrl = cfg.AlphaVantage_ApiUrl;
		});

		services.AddSingleton<IYahooSession, YahooSession>();

		services.AddScoped<IYahooService, YahooService>();
		services.AddScoped<IXetraService, XetraService>();
		services.AddScoped<IAlphaVantageService, AlphaVantageService>();
		services.AddScoped<IOpenDataService, OpenDataService>();

		services.AddHttpClient(cfg.Yahoo_Http_ClientName, client =>
		{
			var userAgent = Helper.CreateRandomUserAgent();
			//client.DefaultRequestHeaders.Add("User-Agent", userAgent);

			client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36");
			client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml,application/json;q=0.9,*/*;q=0.8");
			client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");

			client.Timeout = TimeSpan.FromSeconds(cfg.Http_Timeout);
		});

		services.AddHttpClient(cfg.Xetra_Http_ClientName, client =>
		{
			var userAgent = Helper.CreateRandomUserAgent();
			client.DefaultRequestHeaders.Add("User-Agent", userAgent);
			client.Timeout = TimeSpan.FromSeconds(cfg.Http_Timeout);
		});

		services.AddHttpClient(cfg.AlphaVantage_Http_ClientName, client =>
		{
			var userAgent = Helper.CreateRandomUserAgent();
			client.DefaultRequestHeaders.Add("User-Agent", userAgent);
			client.Timeout = TimeSpan.FromSeconds(cfg.Http_Timeout);
		});

		services.AddHttpClient(cfg.OpenData_Http_ClientName, client =>
		{
			var userAgent = Helper.CreateRandomUserAgent();
			client.DefaultRequestHeaders.Add("User-Agent", userAgent);
			client.Timeout = TimeSpan.FromSeconds(cfg.Http_Timeout);
		});
	}
}
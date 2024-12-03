using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetFinance.Application.Services;
using NetFinance.Application.Utilities;

namespace NetFinance.Application.Extensions;

public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Configures .NetFinanceService
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <param name="configuration">Optional: Default values to configure NetFinanceService. <see cref="NetFinanceOptions"/> ></param>
	public static void AddNetFinance(this IServiceCollection services, IConfiguration? configuration = null)
	{
		services.AddSingleton<IYahooSession, YahooSession>();
		services.AddScoped<INetFinanceService, NetFinanceService>();

		services.AddHttpClient(Constants.ApiClientName, (serviceProvider, client) =>
		{
			var userAgent = Helper.CreateRandomUserAgent();
			client.DefaultRequestHeaders.Add("User-Agent", userAgent);
			client.Timeout = TimeSpan.FromSeconds(Constants.Timeout);
		});
	}
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NetFinance.Exceptions;
using NetFinance.Extensions;
using NetFinance.Interfaces;
using NetFinance.Models.Xetra;
using NetFinance.Models.Xetra.Dto;
using NetFinance.Utilities;

namespace NetFinance.Services;

internal class XetraService : IXetraService
{
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly NetFinanceConfiguration _options;
	private readonly IMapper _mapper;
	private static ServiceProvider? _serviceProvider = null;

	public XetraService(IHttpClientFactory httpClientFactory, IOptions<NetFinanceConfiguration> options)
	{
		_httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
		_options = options?.Value ?? throw new ArgumentNullException(nameof(options));

		// do not use IoC, so users can use Automapper independently
		var config = new MapperConfiguration(cfg => cfg.AddProfile<XetraInstrumentAutomapperProfile>());
		_mapper = config.CreateMapper();
	}

	/// <summary>
	/// Creates a service for interacting with the Xetra API.
	/// Provides methods for retrieving tradable instruments, market data, and other relevant information from Xetra.
	/// </summary>
	/// <param name="cfg">Optional: Default values to configure .Net Finance. <see cref="NetFinanceConfiguration"/> ></param>
	public static IXetraService Create(NetFinanceConfiguration? cfg = null)
	{
		if (_serviceProvider == null)
		{
			var services = new ServiceCollection();
			services.AddNetFinance(cfg);
			_serviceProvider = services.BuildServiceProvider();
		}
		return _serviceProvider.GetRequiredService<IXetraService>();
	}

	public async Task<IEnumerable<Instrument>> GetInstruments(CancellationToken token = default)
	{
		try
		{
			using var httpClient = _httpClientFactory.CreateClient(_options.Xetra_Http_ClientName);
			var response = await httpClient.GetAsync(_options.Xetra_DownloadUrl_Instruments, token);
			response.EnsureSuccessStatusCode();
			var config = new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				Delimiter = ";",
			};

			using var reader = new StreamReader(await response.Content.ReadAsStreamAsync());
			using var csv = new CsvReader(reader, config);
			csv.Read();
			csv.Read();
			csv.Context.RegisterClassMap<XetraInstrumentsMapping>();
			var records = csv.GetRecords<InstrumentItem>()?.ToList();

			return _mapper.Map<List<Instrument>>(records);
		}
		catch (Exception ex)
		{
			throw new NetFinanceException($"Failed to download from {_options.Xetra_DownloadUrl_Instruments}", ex);
		}
	}
}


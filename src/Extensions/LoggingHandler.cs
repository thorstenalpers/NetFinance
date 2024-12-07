using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NetFinance.Extensions;

public class LoggingHandler : DelegatingHandler
{
	private readonly ILogger<LoggingHandler> _logger;

	public LoggingHandler(ILogger<LoggingHandler> logger)
	{
		_logger = logger;
	}

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		// Log request headers
		var requestHeaders = string.Join("; ", request.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"));
		_logger.LogInformation("Outgoing Request Headers: {Headers}", requestHeaders);

		// Send the request
		var response = await base.SendAsync(request, cancellationToken);

		// Log response headers
		var responseHeaders = string.Join("; ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"));
		_logger.LogInformation("Incoming Response Headers: {Headers}", responseHeaders);

		return response;
	}
}

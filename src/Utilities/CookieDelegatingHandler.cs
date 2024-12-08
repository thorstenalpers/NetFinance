//using System;
//using System.Net;
//using System.Net.Http;
//using System.Threading;
//using System.Threading.Tasks;

//namespace NetFinance.Utilities;

//public class CookieDelegatingHandler : DelegatingHandler
//{
//	private readonly CookieContainer _cookieContainer1;
//	private readonly CookieContainer _cookieContainer2;

//	public CookieDelegatingHandler()
//	{
//		_cookieContainer1 = new CookieContainer();
//		_cookieContainer2 = new CookieContainer();
//	}

//	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
//	{
//		CookieContainer selectedContainer = request.RequestUri.Host.Contains("example.com")
//			? _cookieContainer1
//			: _cookieContainer2;

//		var handler = new HttpClientHandler
//		{
//			CookieContainer = selectedContainer
//		};

//		//var customClient = new HttpClient(handler)
//		//{
//		//	Timeout = InnerHandler is HttpClientHandler httpHandler ? httpHandler : Timeout.InfiniteTimeSpan
//		//};

//		return customClient.SendAsync(request, cancellationToken);
//	}
//}

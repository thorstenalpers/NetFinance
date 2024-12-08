using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NetFinance.Interfaces
{
	public interface IYahooSession
	{
		Task RefreshSessionAsync(CancellationToken token = default, bool forceRefresh = false);
		string GetCrumb();
		string GetUserAgent();
		bool AreCookiesValid();
		Task<CookieCollection> GetAndRefreshApiCookies(CancellationToken token = default);
		Task<CookieCollection> GetAndRefreshUiCookies(CancellationToken token = default);
	}
}
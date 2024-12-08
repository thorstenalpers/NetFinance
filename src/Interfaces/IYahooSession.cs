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
		CookieCollection GetApiCookieCollection();
		CookieCollection GetUiCookieCollection();
		bool AreCookiesValid();
	}
}
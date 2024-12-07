using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NetFinance.Interfaces
{
	public interface IYahooSession
	{
		Task RefreshSessionAsync(CancellationToken token = default, bool forceRefresh = false);
		string GetCrumb();
		CookieCollection GetCookieCollection();
		bool AreCookiesValid();
	}
}
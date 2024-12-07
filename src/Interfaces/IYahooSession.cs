using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NetFinance.Interfaces
{
	public interface IYahooSession
	{
		Task<(string crumb, Cookie cookie)> GetSessionAsync(CancellationToken token = default, bool forceRefresh = false);
		Task<(string crumb, Cookie cookie)> RefreshSessionAsync(CancellationToken token = default);
	}
}
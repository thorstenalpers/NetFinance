using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NetFinance.Interfaces
{
	public interface IYahooSession
	{
		Task<(string crumb, Cookie cookie)> GetSessionStateAsync(CancellationToken token = default);
	}
}
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetFinance.Models.Xetra;

namespace NetFinance.Interfaces;

/// <summary>
/// Represents a service for interacting with the Xetra API.
/// Provides methods for retrieving tradable instruments, market data, and other relevant information from Xetra.
/// </summary>
public interface IXetraService
{
	/// <summary>
	/// Asynchronously retrieves a list of tradable instruments from Xetra.com.
	/// </summary>
	/// <param name="token">A <see cref="CancellationToken"/> to allow cancellation of the operation.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains an enumerable collection of <see cref="Instrument"/> objects representing tradable instruments.</returns>
	Task<IEnumerable<Instrument>> GetInstruments(CancellationToken token = default);
}
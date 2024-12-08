using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetFinance.Models.DatahubIo;

namespace NetFinance.Interfaces;

/// <summary>
/// Represents a service for interacting with the DataHubIo API.
/// Provides methods for retrieving financial instruments, market data, and other relevant information from DataHubIo.
/// </summary>
public interface IDatahubIoService
{
	/// <summary>
	/// Asynchronously retrieves a list of Nasdaq instruments from the DataHubIo API.
	/// </summary>
	/// <param name="token">A <see cref="CancellationToken"/> to allow cancellation of the operation.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains an enumerable collection of <see cref="NasdaqInstrument"/> objects representing Nasdaq instruments.</returns>
	Task<IEnumerable<NasdaqInstrument>> GetNasdaqInstrumentsAsync(CancellationToken token = default);

	/// <summary>
	/// Asynchronously retrieves a list of S&P 500 instruments from the DataHubIo API.
	/// </summary>
	/// <param name="token">A <see cref="CancellationToken"/> to allow cancellation of the operation.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains an enumerable collection of <see cref="SP500Instrument"/> objects representing S&P 500 instruments.</returns>
	Task<IEnumerable<SP500Instrument>> GetSAndP500InstrumentsAsync(CancellationToken token = default);
}
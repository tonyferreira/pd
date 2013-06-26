using System;
using System.Threading;
using System.Threading.Tasks;

namespace PD.Core
{
    /// <summary>
    /// Interface to strategize on calculating directory sizes recursively.
    /// </summary>
    public interface IDirectorySizeCalculator
    {
        /// <summary>
        /// Async programming interface allowing for async/await pattern
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <returns></returns>
        Task<long> CalculateSizeAsync(string directoryPath, CancellationTokenSource tokenSource, IProgress<long> progress);
    }

}

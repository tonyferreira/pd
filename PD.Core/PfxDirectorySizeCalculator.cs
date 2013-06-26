using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PD.Core
{
    /// <summary>
    /// Implementation of IDirectorySizeCalculator that uses Pfx's ParallelFor<T>
    /// to find the size of a directory recursively
    /// </summary>
    public sealed class PfxDirectorySizeCalculator : IDirectorySizeCalculator
    {
        // Delegate to handle exceptions.
        // This should be injected by an IoC container, or some kind of object builder.
        private readonly Action<Exception> ExceptionHandler;

        // For thread safe access to shared resources
        private readonly object locker = new object();

        public PfxDirectorySizeCalculator(Action<Exception> exceptionHandler)
        {
            if (exceptionHandler == null)
            {
                throw new ArgumentNullException("exceptionHandler");
            }

            this.ExceptionHandler = exceptionHandler;
        }

        // Calculate the size of the supplied directory recursively
        public long CalculateSize(string directoryPath, CancellationToken token, IProgress<long> progress)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentNullException("directoryPath");

            if (!Directory.Exists(directoryPath))
            {
                throw new InvalidOperationException(
                    string.Format("Directory {0} does not exist.", directoryPath)
                    );
            }

            long size = 0;

            try
            {
                // Benchmarks show iterating DirectoryInfo more optimal (See Benchmarks)
                var directory = new DirectoryInfo(directoryPath);

                var files = directory.GetFiles();

                foreach (var file in files)
                {
                    // Thread-safe increment
                    lock (locker)
                    {
                        size += file.Length;
                    }
                }

                // Report progress back to caller
                lock (locker)
                {
                    progress.Report(size);
                }

                // Recurse child directories
                var directories = directory.GetDirectories();

                // Needed for cancellation
                var options = new ParallelOptions
                {
                    CancellationToken = token
                };

                // Benchmarks show Parallel.For<T> more performant than PLINQ's .AsParallel()
                Parallel.For<long>(
                    // Start index
                    0,
                    // To index
                    directories.Length,
                    // Initialize local value
                    () => 0,
                    // Body delegate returning local total       
                    (i, loopstate, subtotal) =>
                    {
                        if (options.CancellationToken.IsCancellationRequested)
                            return 0;

                        options.CancellationToken.ThrowIfCancellationRequested();
                        subtotal += this.CalculateSize(directories[i].FullName, token, progress);
                        return subtotal;
                    },
                    // Add local total to grand total (size)
                    localTotal =>
                    {
                        lock (locker)
                        {
                            size += localTotal;
                        }
                    }
                );

                return size;
            }
            catch (AggregateException ex)
            {
                foreach (var innerException in ex.InnerExceptions)
                {
                    ExceptionHandler(innerException);
                }

                return 0;
            }
            catch (Exception ex)
            {
                ExceptionHandler(ex);
                return 0;
            }
        }


        // Allows for async/await pattern
        public Task<long> CalculateSizeAsync(string directoryPath, CancellationToken token, IProgress<long> progress)
        {
            // Delegate to synchronous method on background thread
            return Task.Run(() => this.CalculateSize(directoryPath, token, progress));
        }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PD.Core;


namespace PD.ConsoleApplication
{
    class Program
    {
        private static long totalSize = 0;

        //TODO: Retrieve configuration delegates from IoC container.

        private static readonly CancellationToken token = new CancellationTokenSource().Token;

        private static readonly Progress<long> progressCallback = new Progress<long>(/* do nothing for now */);

        // Function delegate used to calculate size of directory
        private static readonly Func<string, CancellationToken, IProgress<long>, Task<long>> calculate =
            new PfxDirectorySizeCalculator(ExceptionHandler).CalculateSizeAsync;

        static void Main(string[] args)
        {
            try
            {

                if (args.Length == 0)
                {
                    Console.WriteLine(
                        @"No arguments supplied.  Please enter up to three directories separated by a space. " +
                        Environment.NewLine +
                        @"For example: ""C:\Program Files"" ""C:\Windows\Temp"" ""C:\Users\{username}"""
                    );

                    return;
                }

                if (args.Length > 3)
                {
                    Console.WriteLine(
                        "Too many directories supplied.  Please enter up to three directories separated by a space.");
                    return;
                }

                var totalSize = GetTotalSize(args).Result;

                Console.WriteLine(
                    "{0} bytes \r\n{1} MB \r\n{2} GB",
                    String.Format("{0:N}", totalSize),
                    String.Format("{0:N2}", totalSize.BytesToMB()),
                    String.Format("{0:N2}", totalSize.BytesToGB())
                    );
            }
            catch (Exception ex)
            {
                ExceptionHandler(ex);
            }
        }

        private static async Task<long> GetTotalSize(string[] directories)
        {
            foreach (var dir in directories)
            {
                Console.WriteLine("Calculating total size of: {0}", dir);
            }

            // Create the tasks to run in parallel in a scatter/gather pattern.
            var calculationTasks = directories.Select(
                async dir => await calculate(dir, token, progressCallback)
            );

            // Wait for Tasks to complete, then join them back together, summing the result from each.
            var results = await Task.WhenAll(calculationTasks);

            return results.Sum();            
        }      

        private static void ExceptionHandler(Exception ex)
        {
            if (ex is PathTooLongException)
            {
                // Do nothing
                // Would normally log, etc. based on global Exception handling policy.
                return;
            }

            if (ex is UnauthorizedAccessException)
            {
                return;
            }

            Console.WriteLine(ex.Message);
        }
    }
}

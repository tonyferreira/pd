using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace PD.Benchmarks
{
    /// <summary>
    /// Benchmark tests used to find the optimized solution for
    /// recursively iterating directories to calc size.
    /// </summary>
    [TestFixture]
    public class BenchmarkTests
    {
        private const string Root = @"C:\Program Files";
        private Stopwatch stopWatch;
        private readonly DirectoryInfo DirInfo = new DirectoryInfo(Root);

        [SetUp]
        public void Setup()
        {
            stopWatch = Stopwatch.StartNew();
        }

        [TearDown]
        public void Teardown()
        {
            var elapsed = stopWatch.ElapsedMilliseconds;
            Console.WriteLine(elapsed);
            stopWatch = null;
        }

        [Test, Ignore]
        public void OleSchoolTest()
        {
            Console.WriteLine("OleSchool");
            Console.WriteLine(this.OleSchool(Root));
        }

        public long OleSchool(string path)
        {

            long size = 0;

            var files = Directory.GetFiles(path);

            for (var index = 0; index < files.Length; index++)
            {
                size += new FileInfo(files[index]).Length;
            }

            var directories = Directory.GetDirectories(path);

            for (var index = 0; index < directories.Length; index++)
            {
                size += this.OleSchool(directories[index]);
            }

            return size;
        }

        [Test, Ignore]
        public void Linq_1_Test()
        {
            Console.WriteLine("Linq_1");
            Console.WriteLine(this.Linq_1(Root));
        }

        public long Linq_1(string path)
        {
            var size = (from file in Directory.GetFiles(path) select new FileInfo(file).Length).Sum();
            size += (from directory in Directory.GetDirectories(path) select Linq_1(directory)).Sum();
            return size;
        }

        [Test, Ignore]
        public void Linq_2_Test()
        {
            Console.WriteLine("Linq_2_Test");
            Console.WriteLine(this.Linq_2(Root));
        }

        public long Linq_2(string path)
        {
            var size = Directory.GetFiles(path).Sum(file => new FileInfo(file).Length);
            size += DirInfo.GetDirectories()
                              .Sum(directory => this.OleSchool(directory.FullName));

            return size;
        }

        [Test, Ignore]
        public void PLinq_1_Test()
        {
            Console.WriteLine("PLinq_1_Test");
            Console.WriteLine(PLinq_1(Root));
        }

        public long PLinq_1(string path)
        {
            var size = (from file in Directory.GetFiles(path)
                                              .AsParallel()
                        select new FileInfo(file).Length).Sum();

            size += (from directory in Directory.GetDirectories(path)
                                                .AsParallel()
                     select Linq_1(directory)).Sum();
            return size;
        }

        [Test, Ignore]
        public void PLinq_2_Test()
        {
            Console.WriteLine("PLinq_2_Test");
            Console.WriteLine(PLinq_2(Root));
        }

        public long PLinq_2(string path)
        {
            var size = Directory.GetFiles(path)
                                .AsParallel()
                                .Sum(file => new FileInfo(file).Length);

            size += DirInfo.GetDirectories()
                               .AsParallel()
                              .Sum(directory => this.OleSchool(directory.FullName));

            return size;
        }

        [Test]
        public void ParallelForTest()
        {
            Console.WriteLine("ParallelForTest");
            Console.WriteLine(this.ParallelFor(Root));
        }

        public long ParallelFor(string path)
        {
            long size = 0;

            var files = Directory.GetFiles(path);

            foreach (var file in files)
            {
                Interlocked.Add(ref size, (new FileInfo(file)).Length);
            }

            var directories = Directory.GetDirectories(path);

            Parallel.For<long>(0, directories.Length, () => 0, (index, loopState, subtotal) =>
            {
                if ((File.GetAttributes(directories[index]) & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                {
                    subtotal += this.ParallelFor(directories[index]);
                    return subtotal;
                }

                return 0;
            },
                increaseBy => Interlocked.Add(ref size, increaseBy)
            );

            return size;
        }

        [Test]
        public void ParallelForUsingDirInfoTest()
        {
            Console.WriteLine("ParallelForUsingDirInfoTest");
            Console.WriteLine(this.ParallelForUsingDirInfo(Root));
        }

        public long ParallelForUsingDirInfo(string path)
        {
            long size = 0;
            var dirInfo = new DirectoryInfo(path);
            var files   = dirInfo.GetFiles();

            foreach (var file in files)
            {
                Interlocked.Add(ref size, file.Length);
            }

            var directories = dirInfo.GetDirectories();

            Parallel.For<long>(0, directories.Length, () => 0, (index, loopState, subtotal) => 
            {
                if ((File.GetAttributes(directories[index].FullName) & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                {
                    subtotal += this.ParallelForUsingDirInfo(directories[index].FullName);
                    return subtotal;
                }

                return 0;
            },
                increaseBy => Interlocked.Add(ref size, increaseBy)
            );

            return size;
        }
    }
}

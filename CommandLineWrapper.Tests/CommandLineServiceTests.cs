using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CommandLineWrapper.Tests
{
    public class CommandLineServiceTest
    {
        private const string executable = @"ls";

        [Fact]
        public async Task RunAsyncTest()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            var arg = path;
            var service = new CommandLineService(executable, arg);
            var timeout = TimeSpan.FromMilliseconds(500);
            var actual = await service.RunAsync(new Progress<string>(str => Debug.WriteLine(str)),
                                                new Progress<string>(str => Debug.WriteLine(str)), timeout,
                                                new CancellationToken());
            const int expected = 0;
            Assert.Equal(expected, actual);
        }
    }
}
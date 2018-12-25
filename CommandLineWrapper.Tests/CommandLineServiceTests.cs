using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CommandLineWrapper.Tests
{
    public class CommandLineServiceTest
    {
        private const string executable = @"C:\Windows\System32\cmd.exe";

        [Fact]
        public async Task RunAsyncTest()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TestFile.txt");
            var arg = $"/C copy /b nul  {path}";
            var service = new CommandLineService(executable, arg, new CancellationTokenSource());
            var timeout = TimeSpan.FromMilliseconds(500);
            await service.RunAsync(null, null, timeout);
            Assert.Equal(File.Exists(path), true);
        }
    }
}
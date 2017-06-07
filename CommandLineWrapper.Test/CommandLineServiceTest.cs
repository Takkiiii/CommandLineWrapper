using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CommandLineWrapper.Test
{
    public class CommandLineServiceTest
    {
        private const string executable = @"C:\Windows\System32\cmd.exe";

        [Fact]
        public async Task ExecuteTest1()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TestFile.txt");
            var arg = $"/C copy /b nul  {path}";
            var service = new CommandLineService(executable, arg, new CancellationTokenSource());
            var timeout = TimeSpan.FromMilliseconds(500);
            await service.ExecuteAsync(null, null, timeout);
            Assert.Equal(File.Exists(path), true);
        }

        [Fact]
        public async Task ExecuteTest2()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TestFile.txt");
            var arg = $"/C del  {path}";
            var service = new CommandLineService(executable, arg, new CancellationTokenSource());
            var timeout = TimeSpan.FromMilliseconds(500);
            await service.ExecuteAsync(null, null, timeout);
            Assert.Equal(File.Exists(path), false);
        }
    }
}

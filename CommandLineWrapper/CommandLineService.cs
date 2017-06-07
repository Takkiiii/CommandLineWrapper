using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CommandLineWrapper
{
    public class CommandLineService : IDisposable
    {
        public CommandLineService(string executable, string arguments, CancellationTokenSource tokenSource)
        {
            Executable = executable;
            Arguments = arguments;
            TokenSource = tokenSource;
        }

        public async Task ExecuteAsync(IProgress<string> stdout, IProgress<string> stderr, TimeSpan timeOut)
        {
            await Task.Run(() =>
            {
                var startInfo = CreateStartInfo(Executable, Arguments);
                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                        throw new NullReferenceException(nameof(process));
                    TokenSource.Token.Register(() =>
                    {
                        process?.Kill();
                        stderr?.Report("Process is canceled.");
                    });
                    process.OutputDataReceived += (_, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                            stdout?.Report(e.Data);
                    };
                    process.ErrorDataReceived += (_, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                            stderr?.Report(e.Data);
                    };
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.EnableRaisingEvents = true;

                    var isTimedOut = false;
                    if (!process.WaitForExit((int)timeOut.TotalMilliseconds))
                    {
                        isTimedOut = true;
                        process.Kill();
                    }
                    process.CancelErrorRead();
                    process.CancelOutputRead();
                    if (isTimedOut)
                        throw new TimeoutException();
                }
            });
        }

        public void Cancel() => TokenSource.Cancel();

        public string Executable { get; }

        public string Arguments { get; }

        public CancellationTokenSource TokenSource { get; }

        private static ProcessStartInfo CreateStartInfo(string executable, string arguments) => new ProcessStartInfo(
            executable, arguments)
        {
            WorkingDirectory = Environment.CurrentDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        public void Dispose()
        {
            TokenSource.Dispose();
        }
    }
}

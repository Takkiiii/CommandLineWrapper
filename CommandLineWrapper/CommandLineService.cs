
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CommandLineWrapper
{
    public class CommandLineService : IDisposable
    {
        /// <summary>
        ///     コンストラクタ
        /// </summary>
        /// <param name="executable">実行ファイル</param>
        /// <param name="arguments">引数</param>
        /// <param name="tokenSource">キャンセルトークン</param>
        public CommandLineService(string executable, string arguments, CancellationTokenSource tokenSource)
        {
            Executable = executable;
            Arguments = arguments;
            TokenSource = tokenSource;
        }

        /// <summary>
        ///     実行ファイルを実行します
        /// </summary>
        /// <param name="stdout">標準出力</param>
        /// <param name="stderr">エラー出力</param>
        /// <param name="timeOut">タイムアウト時間</param>
        /// <returns></returns>
        public Task<int> RunAsync(IProgress<string> stdout, IProgress<string> stderr, TimeSpan timeOut)
        {
            var tcs = new TaskCompletionSource<int>();
            var startInfo = CreateStartInfo(Executable, Arguments);
            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    throw new NullReferenceException(nameof(process));
                }
                    
                TokenSource.Token.Register(() =>
                {
                    process?.Kill();
                    throw new OperationCanceledException();
                });
                process.OutputDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        stdout?.Report(e.Data);
                    }      
                };
                process.ErrorDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        stderr?.Report(e.Data);
                    }
                };
                process.Exited += (sender, args) =>
                {
                    tcs.SetResult(process.ExitCode);
                };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.EnableRaisingEvents = true;

                var isTimedOut = false;
                if (!process.WaitForExit((int)timeOut.TotalMilliseconds))
                {
                    isTimedOut = true;
                    process?.Kill();
                }
                process.CancelErrorRead();
                process.CancelOutputRead();
                if (isTimedOut)
                {
                    throw new TimeoutException();
                }
            }
            return tcs.Task;
        }

        /// <summary>
        ///     プロセスをキャンセルします
        /// </summary>
        public void Cancel()
        {
            TokenSource.Cancel();
        }

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
            Executable = null;
            Arguments = null;
            TokenSource.Dispose();
        }

        public string Executable { get; private set;}

        public string Arguments { get; private set;}

        public CancellationTokenSource TokenSource { get; }

    }
}
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CommandLineWrapper
{
    internal interface IService<TRet>
    {
        Task<TRet> RunAsync(IProgress<string> stdout, IProgress<string> stderr, TimeSpan timeOut,
            CancellationToken cancel);
    }
    
    public sealed class CommandLineService : IService<int?>
    {
        /// <summary>
        ///     コンストラクタ
        /// </summary>
        /// <param name="executable">実行ファイル</param>
        /// <param name="arguments">引数</param>
        public CommandLineService(string executable, string arguments)
        {
            this.executable = executable;
            this.arguments = arguments;
        }

        /// <summary>
        ///     実行ファイルを実行します
        /// </summary>
        /// <param name="stdout">標準出力</param>
        /// <param name="stderr">エラー出力</param>
        /// <param name="timeOut">タイムアウト時間</param>
        /// <returns></returns>
        public Task<int?> RunAsync(IProgress<string> stdout, IProgress<string> stderr, TimeSpan timeOut,
            CancellationToken cancel)
        {
            var tcs = new TaskCompletionSource<int?>();
            var startInfo = CreateStartInfo(executable, arguments);
            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    throw new NullReferenceException(nameof(process));
                }

                var registration = cancel.Register(() =>
                                           {
                                               if (process == null)
                                               {
                                                   return;;
                                               }

                                               process?.Kill();
                                               throw new OperationCanceledException();
                                           });
                process.OutputDataReceived += (_, e) =>
                                              {
                                                  if (string.IsNullOrEmpty(e.Data))
                                                  {
                                                      return;
                                                  }
                                                  stdout?.Report(e.Data);
                                              };
                process.ErrorDataReceived += (_, e) =>
                                             {
                                                 if (string.IsNullOrEmpty(e.Data))
                                                 {
                                                     return;
                                                 }
                                                 stderr?.Report(e.Data);
                                             };
                process.Exited += (_, __) =>
                                  {
                                      registration.Dispose();
                                      tcs.SetResult(process?.ExitCode);
                                  };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.EnableRaisingEvents = true;

                var isTimedOut = false;
                if (!process.WaitForExit((int) timeOut.TotalMilliseconds))
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

        private static ProcessStartInfo CreateStartInfo(string executable, string arguments)
        {
            return new ProcessStartInfo(
                                        executable, arguments)
                   {
                       WorkingDirectory = Environment.CurrentDirectory,
                       RedirectStandardOutput = true,
                       RedirectStandardError = true,
                       UseShellExecute = false,
                       CreateNoWindow = true
                   };
        }

        private readonly string executable;

        private readonly string arguments;
    }
}
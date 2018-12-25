# CommandLineWrapper

Command Line Wrapper Class

## Usage

```cs
v executable = @"C:\Windows\System32\cmd.exe";
var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TestFile.txt");
var arg = $"/C copy /b nul  {path}";
var service = new CommandLineService(executable, arg, new CancellationTokenSource());
var timeout = TimeSpan.FromMilliseconds(500);
await service.ExecuteAsync(null, null, timeout);
```

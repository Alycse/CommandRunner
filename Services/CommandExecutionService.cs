using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommandRunner.Models;
using CommandRunner.ViewModels;

namespace CommandRunner.Services
{
    public class CommandExecutionService
    {
        public async Task ExecuteCommandAsync(string name, Command command, Action<ProcessViewModel> onProcessStarted, Action<ProcessViewModel> onProcessCompleted, Action<string> onLogReceived)
        {
            if (command == null || string.IsNullOrWhiteSpace(command.FilePath))
                return;

            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = command.FilePath,
                    Arguments = command.Argument,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var process = new Process { StartInfo = processStartInfo, EnableRaisingEvents = true };

                var processViewModel = new ProcessViewModel
                {
                    Command = command,
                    Name = name,
                    Process = process
                };

                onProcessStarted?.Invoke(processViewModel);

                process.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        onLogReceived?.Invoke(args.Data);
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        onLogReceived?.Invoke(args.Data);
                    }
                };

                process.Exited += (sender, args) =>
                {
                    processViewModel.IsEnded = true; // Mark as ended
                    onProcessCompleted?.Invoke(processViewModel);
                    process.Dispose();
                };

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await Task.Run(() => process.WaitForExit());
            }
            catch (Exception ex)
            {
                onLogReceived?.Invoke($"Error: {ex.Message}");
            }
        }
    }
}
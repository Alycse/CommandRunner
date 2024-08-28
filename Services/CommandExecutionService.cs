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
                    RedirectStandardOutput = command.TrackProcess,  // Only redirect output if tracking
                    RedirectStandardError = command.TrackProcess,   // Only redirect error if tracking
                    UseShellExecute = !command.TrackProcess,        // Use shell execute if not tracking
                    CreateNoWindow = command.TrackProcess           // Show window only if not tracking
                };

                var process = new Process { StartInfo = processStartInfo, EnableRaisingEvents = true };

                ProcessViewModel processViewModel = null;

                if (command.TrackProcess)
                {
                    processViewModel = new ProcessViewModel
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
                }

                process.Exited += (sender, args) =>
                {
                    if (command.TrackProcess)
                    {
                        processViewModel.IsEnded = true; // Mark as ended
                        onProcessCompleted?.Invoke(processViewModel);
                    }
                    process.Dispose();
                };

                process.Start();

                if (command.TrackProcess)
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }

                await Task.Run(() => process.WaitForExit());

                if (!command.TrackProcess)
                {
                    // Command executed without tracking, do nothing further
                    onLogReceived?.Invoke($"Command '{name}' executed and completed without tracking.");
                }
            }
            catch (Exception ex)
            {
                if (command.TrackProcess || string.IsNullOrWhiteSpace(command.FilePath))
                {
                    onLogReceived?.Invoke($"Error: {ex.Message}");
                }
                else
                {
                    // If not tracking and an error occurs, just write a basic error message
                    onLogReceived?.Invoke($"Command '{name}' execution failed.");
                }
            }
        }
    }
}
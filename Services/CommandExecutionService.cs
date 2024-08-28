﻿using System;
using System.Diagnostics;
using System.IO;
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

            // Declare processViewModel at the start of the method
            ProcessViewModel processViewModel = null;

            // Task completion source to signal when the log is detected
            var logDetected = new TaskCompletionSource<bool>();

            // Initialize the logTextChangedHandler here so it's in scope
            EventHandler logTextChangedHandler = null;

            try
            {
                var workingDirectory = Path.GetDirectoryName(command.FilePath);

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = command.FilePath,
                    Arguments = command.Argument,
                    RedirectStandardOutput = command.TrackProcess,  // Only redirect output if tracking
                    RedirectStandardError = command.TrackProcess,   // Only redirect error if tracking
                    UseShellExecute = !command.TrackProcess,        // Use shell execute if not tracking
                    CreateNoWindow = command.TrackProcess,          // Show window only if not tracking
                    WorkingDirectory = workingDirectory
                };

                var process = new Process { StartInfo = processStartInfo, EnableRaisingEvents = true };

                logTextChangedHandler = (sender, args) =>
                {
                    if (processViewModel != null && processViewModel.LogText.Contains(command.LogToDetectBeforeContinuing))
                    {
                        logDetected.TrySetResult(true);
                    }
                };

                if (command.TrackProcess)
                {
                    processViewModel = new ProcessViewModel
                    {
                        Command = command,
                        Name = name,
                        Process = process
                    };

                    processViewModel.OnLogTextChanged += logTextChangedHandler;

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

                // Await process completion or log detection
                await Task.WhenAny(Task.Run(() => process.WaitForExit()), logDetected.Task);

                // Invoke completion event if log was detected
                if (logDetected.Task.IsCompleted && processViewModel != null)
                {
                    onProcessCompleted?.Invoke(processViewModel);
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
            finally
            {
                // Ensure event handler is removed to prevent memory leaks
                if (processViewModel != null)
                {
                    processViewModel.OnLogTextChanged -= logTextChangedHandler;
                }
            }
        }
    }
}
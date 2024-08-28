using System.Diagnostics;
using CommandRunner.Models;

namespace CommandRunner.Services
{
    public class CommandExecutionService
    {
        public void ExecuteCommand(Command command)
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
                    CreateNoWindow = false
                };

                using (var process = new Process { StartInfo = processStartInfo })
                {
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    // Log or handle the output and errors as needed
                    if (!string.IsNullOrEmpty(output))
                    {
                        // Handle output
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
                        // Handle errors
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions, log or display error messages
            }
        }
    }
}
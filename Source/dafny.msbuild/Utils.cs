using System.Collections.Generic;
using System.Diagnostics;

namespace DafnyMSBuild
{
    internal static class Utils
    {
        /**
         * <summary>
         * Runs the executable with arguments in the given working directory, waits for the process to exit,
         * and returns the Process.
         * The <paramref name="stdout"/> out-parameter is set to the full standard output.
         * The caller is responsible for disposing of the returned Process.
         * </summary>
         * <example>
         * <code>
         * using var process = RunProcess("git", new[] { "rev-parse", "HEAD" }, out var stdout, ".");
         * var headHash = process.ExitCode == 0 ? stdout : null;
         * </code>
         * </example>
         */
        public static Process RunProcess(string executable, IEnumerable<string> args, out string stdout, string workingDir = "")
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = executable,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WorkingDirectory = workingDir,
                    RedirectStandardOutput = true
                }
            };
            foreach (var arg in args)
            {
                process.StartInfo.ArgumentList.Add(arg);
            }
            process.Start();

            // We must read before waiting, or else deadlock is possible.
            // See <https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.standardoutput>.
            stdout = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return process;
        }
    }
}

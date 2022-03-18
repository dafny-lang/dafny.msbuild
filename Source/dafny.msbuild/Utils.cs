using System.Collections.Generic;
using System.Diagnostics;

namespace DafnyMSBuild
{
    internal static class Utils
    {
        /**
         * Runs the executable with arguments in the given working directory, and returns the Process.
         * The caller is responsible for disposing of the returned Process.
         * <example>
         * <code>
         * using var process = RunProcess("git", new[] { "rev-parse", "HEAD" }, ".");
         * var headHash = process.ExitCode == 0 ? process.StandardOutput.ReadLine() : null;
         * </code>
         * </example>
         */
        public static Process RunProcess(string executable, IEnumerable<string> args, string workingDir = "")
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
            foreach (var arg in args) process.StartInfo.ArgumentList.Add(arg);
            process.Start();
            process.WaitForExit();
            return process;
        }
    }
}

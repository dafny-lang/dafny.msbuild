using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace DafnyMSBuild
{
    /**
     * A custom Task for checking that the provided Dafny CLI is of a compatible version.
     */
    public class DafnyVersionCheckTask : Task
    {
        public string DafnyBinariesDir { get; set; }

        [Required]
        public string DafnyExecutablePath { get; set; }

        // Either a Dafny version number, like "2.3.0.10506", or the empty string.
        public string DafnyVersion { get; set; }

        // Either a Git commit hash, or the empty string.
        public string DafnyVersionCommit { get; set; }

        // Either a Git tag name, or the empty string.
        public string DafnyVersionTag { get; set; }

        public override bool Execute()
        {
            var foundDafnyVersion = GetDafnyExecutableVersion(DafnyExecutablePath);

            var enforceVersion = !string.IsNullOrEmpty(DafnyVersion);
            var enforceCommit = !string.IsNullOrEmpty(DafnyVersionCommit);
            var enforceTag = !string.IsNullOrEmpty(DafnyVersionTag);
            if (!(enforceVersion || enforceCommit || enforceTag)) return Success(foundDafnyVersion);
            if (new[] {enforceVersion, enforceCommit, enforceTag}.Count(b => b) > 1) {
                return Fail("The DafnyVersion, DafnyVersionCommit, and DafnyVersionTag properties are mutually exclusive");
            }

            if (enforceVersion) return DafnyVersion == foundDafnyVersion
                ? Success(foundDafnyVersion)
                : Fail($"Expected Dafny version \"{DafnyVersion}\", found \"{foundDafnyVersion}\"");

            var expectedRepoHead = enforceCommit ? $"commit {DafnyVersionCommit}" : $"tag {DafnyVersionTag}";
            if (string.IsNullOrEmpty(DafnyBinariesDir))
            {
                return Fail("DafnyBinariesDir must be set to a directory within a Git repo in order to check DafnyVersionCommit or DafnyVersionTag");
            }
            if (IsGitWorkingTreeDirty(DafnyBinariesDir))
            {
                return Fail($"Expected Dafny executable Git repo at {expectedRepoHead}, but working tree or index has changes");
            }

            var specifiedCommitHash = enforceCommit ? DafnyVersionCommit : GetCommitHashForGitTag(DafnyBinariesDir, DafnyVersionTag);
            var repoHeadCommitHash = GetGitRepoHeadCommitHash(DafnyBinariesDir);
            return repoHeadCommitHash != null && repoHeadCommitHash == specifiedCommitHash
                ? Success(foundDafnyVersion)
                : Fail($"Expected Dafny executable Git repo at {expectedRepoHead}, but HEAD is at {repoHeadCommitHash}");
        }

        /**
         * Prints a success message and returns true.
         */
        private bool Success(string version)
        {
            Log.LogMessage(MessageImportance.High, "Found Dafny version {0}", version);
            return true;
        }

        /**
         * Prints the given failure message and returns false.
         */
        private bool Fail(string message)
        {
            Log.LogError(message);
            return false;
        }

        /**
         * Returns the Dafny version (as printed during execution) and full path; or null, if the process does not print its version in a parseable manner.
         */
        private static string GetDafnyExecutableVersion(string dafnyExecutablePath)
        {
            using var process = new Process
            {
                StartInfo = {FileName = dafnyExecutablePath, CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true}
            };
            // Dafny prints its version only if given at least one input file. With an invalid file name, it will print its version, then immediately exit.
            process.StartInfo.ArgumentList.Add("");
            process.Start();
            process.WaitForExit();

            var output = process.StandardOutput.ReadLine() ?? "";
            var match = new Regex("^Dafny ([0-9.]+)$").Match(output);
            return match.Success ? match.Groups[1].Value : null;
        }

        /**
         * Returns the HEAD commit hash of the Git repository containing the working directory, or null if an error occurred.
         */
        private static string GetGitRepoHeadCommitHash(string workingDir)
        {
            using var process = RunGitProcess(workingDir, new[] {"show-ref", "-s", "--verify", "HEAD"});
            return process.ExitCode == 0 ? process.StandardOutput.ReadLine() : null;
        }

        /**
         * Returns the commit hash corresponding to the given tag name, or null if an error occurred.
         */
        private static string GetCommitHashForGitTag(string workingDir, string tagName)
        {
            using var process = RunGitProcess(workingDir, new[] {"show-ref", "-s", "--verify", $"refs/tags/{tagName}"});
            return process.ExitCode == 0 ? process.StandardOutput.ReadLine() : null;
        }

        /**
         * Returns true if the Git repository containing the working directory is dirty (has changes in its index or work tree).
         */
        private static bool IsGitWorkingTreeDirty(string workingDir)
        {
            using var process = RunGitProcess(workingDir, new[] {"diff-index", "--quiet", "HEAD", "--"});
            return process.ExitCode switch
            {
                0 => false,
                1 => true,
                _ => throw new Exception("`git diff-index` failed")
            };
        }

        /**
         * Runs Git in the given working directory and with the given arguments, and returns the Process.
         * This assumes that `git` is in the user's PATH variable.
         * The caller is responsible for disposing of the returned Process.
         */
        private static Process RunGitProcess(string workingDir, IEnumerable<string> args)
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = "git",
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

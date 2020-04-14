using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace DafnyMSBuild
{
    /**
     * A custom task for verifying Dafny source code. Mainly implemented in C#
     * so we can verify in parallel (by leveraging Parallel LINQ, which automatically
     * leverages all available cores).
     */
    public class DafnyCompileTask : Task
    {
        private static string[] REQUIRED_PARAMS = { "timeLimit" };

        [Required]
        public string DafnyExecutable { get; set; }
        
        [Required]
        public ITaskItem[] DafnySourceFiles { get; set; }

        public string Jobs { get; set; }

        [Required]
        public String[] DafnyVerificationParams { get; set; }

        public override bool Execute() {
            Console.WriteLine("executed");
            return true;
        }

        private string[] GetSplitParam(string unsplitParam) {
            var splitParamList = unsplitParam.Split(':');
            if (splitParamList.Length > 2) {
                throw new ArgumentException("Invalid verification argument, multiple :");
            }
            return splitParamList;
        }

        private bool VerifyDafnyFile(ITaskItem file, Dictionary<string, string> verificationParams) {
            Log.LogMessage(MessageImportance.High, "Verifying {0}...", file.ItemSpec);
            
            using (Process verifyProcess = new Process()) {
                verifyProcess.StartInfo.FileName = DafnyExecutable;
                verifyProcess.StartInfo.ArgumentList.Add("/compile:0");
                // Apply all relevant parameters
                foreach (var entry in verificationParams) {
                    if (String.IsNullOrEmpty(entry.Value)) {
                        verifyProcess.StartInfo.ArgumentList.Add(String.Format("/{0}", entry.Key));
                    }
                    else {
                        verifyProcess.StartInfo.ArgumentList.Add(String.Format("/{0}:{1}", entry.Key, entry.Value));
                    }
                }
                verifyProcess.StartInfo.ArgumentList.Add(file.ItemSpec);
                verifyProcess.StartInfo.UseShellExecute = false;
                verifyProcess.StartInfo.RedirectStandardOutput = true;
                verifyProcess.StartInfo.RedirectStandardError = true;
                verifyProcess.StartInfo.CreateNoWindow = true;
                verifyProcess.Start();
                verifyProcess.WaitForExit();
                bool success = verifyProcess.ExitCode == 0;
                
                Log.LogMessage(MessageImportance.High, "Verifying {0} {1}", file.ItemSpec, success ? "succeeded!" : "failed:");
                if (!success) {
                    string output = verifyProcess.StandardOutput.ReadToEnd();
                    Log.LogMessage(MessageImportance.High, output);
                }
                return success;
            }
        }
    }
}

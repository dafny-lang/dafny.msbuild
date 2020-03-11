using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    public class DafnyVerifyTask : Task
    {
        [Required]
        public string DafnyExecutable { get; set; }
        
        [Required]
        public ITaskItem[] DafnySourceFiles { get; set; }

        [Required]
        public string TimeLimit { get; set; }
        
        public string Jobs { get; set; }

        public override bool Execute() {
            ParallelOptions options = new ParallelOptions();
            if (Jobs != null) {
                options.MaxDegreeOfParallelism = int.Parse(Jobs);
            }

            ConcurrentBag<bool> results = new ConcurrentBag<bool>();
            Parallel.ForEach(DafnySourceFiles, options, file => {
                results.Add(VerifyDafnyFile(file));
            });
            return results.All(x => x);
        }

        private bool VerifyDafnyFile(ITaskItem file) {
            Log.LogMessage(MessageImportance.High, "Verifying {0}...", file.ItemSpec);
            
            using (System.Diagnostics.Process verifyProcess = new System.Diagnostics.Process()) {
                verifyProcess.StartInfo.FileName = DafnyExecutable;
                verifyProcess.StartInfo.ArgumentList.Add("/compile:0");
                verifyProcess.StartInfo.ArgumentList.Add("/timeLimit:" + TimeLimit);
                verifyProcess.StartInfo.ArgumentList.Add(file.ItemSpec);
                verifyProcess.StartInfo.UseShellExecute = false;
                verifyProcess.StartInfo.RedirectStandardOutput = true;
                verifyProcess.StartInfo.RedirectStandardError = true;
                verifyProcess.StartInfo.CreateNoWindow = true;
                verifyProcess.Start();
                verifyProcess.WaitForExit();
                bool success = verifyProcess.ExitCode == 0;
                
                Log.LogMessage(MessageImportance.High, "Verifying {0} {1}", file.ItemSpec, success ? "succeeded!" : "failed:");
                if (!success)
                {
                    string output = verifyProcess.StandardOutput.ReadToEnd();
                    Log.LogMessage(MessageImportance.High, output);
                }
                
                return success;
            }
        }
    }
}

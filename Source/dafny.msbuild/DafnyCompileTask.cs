using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
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
        [Required]
        public string DafnyExecutable { get; set; }
        
        [Required]
        public ITaskItem[] DafnySourceFiles { get; set; }

        [Required]
        public string DafnyOutputFile { get; set; }

        public string Jobs { get; set; }

        [Required]
        public String[] DafnyCompileParams { get; set; } 

        public override bool Execute() {   
            // Determine all valid parameters
            var verificationParamsDict = new Dictionary<string, string>();
            foreach (var param in DafnyCompileParams)   
            {
                var keyVal = GetSplitParam(param);
                verificationParamsDict[keyVal[0]] = keyVal.Length == 2 ? keyVal[1] : "";
            }
            
            
            // Determine if the verification task should be performed in parallel
            ParallelOptions options = new ParallelOptions();
            int jobCount;
            if (Jobs != null && int.TryParse(Jobs, out jobCount))
            {
                options.MaxDegreeOfParallelism = jobCount;
            }
            // Verify all files
            ConcurrentBag<bool> results = new ConcurrentBag<bool>();
            Parallel.ForEach(DafnySourceFiles, options, file => {
                results.Add(VerifyDafnyFile(file, verificationParamsDict)); 
            });
            
            return results.All(x => x);
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
                verifyProcess.StartInfo.ArgumentList.Add(
                       String.Format("/out:{0}",DafnyOutputFile));
                verifyProcess.StartInfo.ArgumentList.Add( "/compile:0" );
                verifyProcess.StartInfo.ArgumentList.Add( "/spillTargetCode:3");
                verifyProcess.StartInfo.ArgumentList.Add("/noVerify");
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
                
                Log.LogMessage(MessageImportance.High, "Compiling {0} {1}", file.ItemSpec, success ? "succeeded!" : "failed:");
                if (!success) {
                    string output = verifyProcess.StandardOutput.ReadToEnd();
                    Log.LogMessage(MessageImportance.High, output);
                }
                return success;
            }
        }
    }
}

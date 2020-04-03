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
    public class DafnyVerifyTask : Task
    {
        private static string JOB_KEY = "Jobs";
        private static string[] REQUIRED_PARAMS = { "timeLimit", "compile" };

        [Required]
        public string DafnyExecutable { get; set; }
        
        [Required]
        public ITaskItem[] DafnySourceFiles { get; set; }

        [Required]
        public String[] DafnyVerificationParams { get; set; }

        [Required]
        public String[] DafnySharedParams { get; set; }

        public override bool Execute()
        {
            // Determine all valid parameters, start with shared params and then potentially override with
            // verification-specific params
            var verificationParamsDict = new Dictionary<string, string>();
            foreach (var param in DafnySharedParams) {
                var keyVal = GetSplitParam(param);
                verificationParamsDict[keyVal[0]] = keyVal.Length == 2 ? keyVal[1] : "";
            }
            foreach (var param in DafnyVerificationParams) {
                var keyVal = GetSplitParam(param);
                verificationParamsDict[keyVal[0]] = keyVal.Length == 2 ? keyVal[1] : "";
            }
            foreach (var requiredParam in REQUIRED_PARAMS) {
                if (!verificationParamsDict.ContainsKey(requiredParam)) {
                    throw new ArgumentException(String.Format("{0} required for verification", requiredParam));
                }
            }

            // Determine if the verification task should be performed in parallel
            ParallelOptions options = new ParallelOptions();
            string jobValue = "";
            int jobCount = -1;
            if (verificationParamsDict.TryGetValue(JOB_KEY, out jobValue) && int.TryParse(jobValue, out jobCount)) {
                options.MaxDegreeOfParallelism = jobCount;
            }

            // Verify all files
            ConcurrentBag<bool> results = new ConcurrentBag<bool>();
            Parallel.ForEach(DafnySourceFiles, options, file => {
                results.Add(VerifyDafnyFile(file, verificationParamsDict));
            });
            return results.All(x => x);
        }

        private string[] GetSplitParam(string unsplitParam)
        {
            var splitParamList = unsplitParam.Split(':');
            if (splitParamList.Length > 2) {
                throw new ArgumentException("Invalid verification argument, multiple :");
            }
            return splitParamList;
        }

        private bool VerifyDafnyFile(ITaskItem file, Dictionary<string, string> verificationParams)
        {
            Log.LogMessage(MessageImportance.High, "Verifying {0}...", file.ItemSpec);
            
            using (Process verifyProcess = new Process()) {
                verifyProcess.StartInfo.FileName = DafnyExecutable;
                // Apply all relevant parameters
                foreach (var entry in verificationParams) {
                    if (JOB_KEY.Equals(entry.Key)) {
                        continue;
                    }
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

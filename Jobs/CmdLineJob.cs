using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Quartz;
using quartz_test.Util;
using System.IO;

namespace GenericScheduler.Jobs
{
    [PersistJobDataAfterExecution]
    public class CmdLineJob : IJob
    {
        public const string ExecutableFileJobDataMapKey = "ExecutableFile";
        public const string ArgumentsJobDataMapKey = "Arguments";
        public const string WorkingDirectoryJobDataMapKey = "WorkingDirectory";
        const string ScheduledFireTimeUtcFormat = "{{.ScheduledFireTimeUtc}}";
        const string FireTimeUtcFormat = "{{.FireTimeUtc}}";

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                int exitCode = await Task.Run(async () =>
                {
                    var commandToExec = ParseCommand(context);
                    var commandArgs = ParseCommandArgs(context);
                    var commandWorkingDir = ParseWorkingDirectory(context);

                    return await RunProcessAsync(fileName: commandToExec, args: commandArgs, workingDir: commandWorkingDir);
                });
                Console.WriteLine($"Process executed. Exit code: {exitCode}");
                if (exitCode != 0)
                {
                    var errMsg = $"Process exited abnormally (exit code = {exitCode}). Registering failure.";
                    Console.WriteLine(errMsg);
                    throw new JobExecutionException(
                        cause: new InvalidOperationException(errMsg),
                        refireImmediately: false);
                }
            }
            catch (JobExecutionException jex)
            {
                Console.WriteLine($"Error in Job execution. Cause: {jex.InnerException.Message}");
                throw jex;
            }
            catch (Exception ex) {
                Console.WriteLine($"Error attempting to run process. Cause: {ex.Message}");
                throw new JobExecutionException(
                    cause: ex,
                    refireImmediately: false);
            }
        }


        private string ParseCommand(IJobExecutionContext context)
        {
            var jobDataMap = context.JobDetail.JobDataMap;
            if (!jobDataMap.ContainsKey(ExecutableFileJobDataMapKey))
            {
                throw new JobExecutionException(
                    cause: new ArgumentException($"The {ExecutableFileJobDataMapKey} job argument is not specified"),
                    refireImmediately: false);
            }
            var commandToExec = jobDataMap[ExecutableFileJobDataMapKey]
                .ToString()
                .ReplaceEnvVariables();

            return commandToExec;
        }

        private string ParseCommandArgs(IJobExecutionContext context)
        {
            var jobDataMap = context.JobDetail.JobDataMap;
            var commandArgsTemplate = jobDataMap.ContainsKey(ArgumentsJobDataMapKey) ?
                jobDataMap[ArgumentsJobDataMapKey].ToString() : string.Empty;
            var commandArgs = commandArgsTemplate
                .Replace(ScheduledFireTimeUtcFormat, GetJobScheduledFireTimeUtc(context), StringComparison.InvariantCultureIgnoreCase)
                .Replace(FireTimeUtcFormat, GetJobFireTimeUtc(context), StringComparison.InvariantCultureIgnoreCase)
                .ReplaceEnvVariables();

            return commandArgs;
        }

        private string ParseWorkingDirectory(IJobExecutionContext context)
        {
            var jobDataMap = context.JobDetail.JobDataMap;
            var workingDirectoryTemplate = jobDataMap.ContainsKey(WorkingDirectoryJobDataMapKey) ?
                jobDataMap[WorkingDirectoryJobDataMapKey].ToString() : string.Empty;
            var commandArgs = workingDirectoryTemplate
                .ReplaceEnvVariables();

            return commandArgs;
        }

        private string GetJobScheduledFireTimeUtc(IJobExecutionContext context)
        {
            var jobFireDateTime = context.ScheduledFireTimeUtc != null ?
                context.ScheduledFireTimeUtc?.ToString("o") : GetJobFireTimeUtc(context);
            return jobFireDateTime;
        }

        private string GetJobFireTimeUtc(IJobExecutionContext context)
        {
            return context.FireTimeUtc.ToString("o");
        }

        private static async Task<int> RunProcessAsync(string fileName, string args, string workingDir)
        {
            if (workingDir == null || workingDir.Trim() == string.Empty)
            {
                workingDir = Path.GetDirectoryName(fileName);
            }
            using (var process = new Process
            {
                StartInfo =
                {
                    FileName = fileName, Arguments = args,
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true,
                    WorkingDirectory = workingDir
                },
                EnableRaisingEvents = true
            })
            {
                return await RunProcessAsync(process).ConfigureAwait(false);
            }
        }

        private static Task<int> RunProcessAsync(Process process)
        {
            var tcs = new TaskCompletionSource<int>();

            process.Exited += (s, ea) => tcs.SetResult(process.ExitCode);
            process.OutputDataReceived += (s, ea) =>
            {
                if (ea.Data != null && ea.Data.Length > 0)
                {
                    Console.WriteLine(ea.Data);
                }
            };
            process.ErrorDataReceived += (s, ea) =>
            {
                if (ea.Data != null && ea.Data.Length > 0)
                {
                    Console.WriteLine("ERR: " + ea.Data);
                }
            };

            try
            {
                bool started = process.Start();
                if (!started)
                {
                    throw new InvalidOperationException("Could not start process: " + process);
                }
            }
            catch (Exception ex) {
                throw new JobExecutionException(
                    cause: ex,
                    refireImmediately: false);
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return tcs.Task;
        }
    }
}

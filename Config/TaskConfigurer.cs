using System;
using System.Collections.Generic;
using System.IO;
using GenericScheduler.Jobs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz;
using Quartz.Impl;

namespace quartz_test.Config
{
    public static class TaskConfigurer
    {
        public static void ConfigureTask(IScheduler scheduler, string configFileName)
        {
            if (!File.Exists(configFileName))
            {
                return;
            }

            JObject taskConfig;
            using (var sr = new StreamReader(configFileName))
            {
                var jsonReader = new JsonTextReader(sr);
                taskConfig = JObject.Load(jsonReader);
            }
            var taskDefinitions = (JObject) taskConfig["TaskDefinitions"];
            foreach (JProperty group in taskDefinitions.Properties())
            {
                var groupName = group.Name;
                Console.WriteLine(groupName);
                var jobs = (JObject)group.Value;
                foreach (var job in jobs.Properties())
                {
                    var jobName = job.Name;
                    var jobType = job.Value["Type"].Value<string>();
                    if (jobType == "ProcessExec")
                    {
                        ConfigureProcessExecJob(scheduler, groupName, jobName, (JObject)job.Value);
                    }
                    else
                    {
                        Console.WriteLine($"Unknown job type '{jobType}'. Skipping.");
                    }
                    Console.WriteLine("  " + jobName);
                }
            }
            Console.WriteLine();
        }

        private static void ConfigureProcessExecJob(IScheduler scheduler, string groupName, string jobName, JObject job)
        {
            var jobSettings = (JObject)job["Settings"];
            var executableFile = jobSettings["ExecutableFile"].Value<string>();
            var arguments = jobSettings["Arguments"]?.Value<string>();
            var workingDirectory = jobSettings["WorkingDirectory"]?.Value<string>();

            var jobData = new List<Tuple<string, string>>();
            jobData.Add(new Tuple<string, string>(CmdLineJob.ExecutableFileJobDataMapKey, executableFile));
            if (arguments != null)
            {
                jobData.Add(new Tuple<string, string>(CmdLineJob.ArgumentsJobDataMapKey, arguments));
            }
            if (workingDirectory != null)
            {
                jobData.Add(new Tuple<string, string>(CmdLineJob.WorkingDirectoryJobDataMapKey, workingDirectory));
            }

            var jobDetail = JobBuilder.Create<CmdLineJob>()
                .WithIdentity(group: groupName, name: jobName)
                .StoreDurably(durability: true)
                .RequestRecovery(shouldRecover: true)
                .Build();

            var jobSchedules = (JArray)job["Schedule"];
            foreach (var schedule in jobSchedules)
            {
                var scheduleType = schedule["Type"].ToString().ToUpper();
                switch (scheduleType)
                {
                    case "CRON":
                    ConfigureCronScheduleTask(scheduler, schedule, jobDetail, jobData);
                    break;
                    case "SIMPLE":
                    ConfigureSimpleScheduleTask(scheduler, schedule, jobDetail, jobData);
                    break;
                    default:
                    throw new ArgumentException($"Schedule type {scheduleType} not supported");
                }
            }
        }

        private static void ConfigureCronScheduleTask(IScheduler scheduler, JToken schedule, IJobDetail jobDetail, IList<Tuple<string, string>> jobData)
        {
            var settings = schedule["Settings"];
            var cronExpr = settings.Value<string>("Expr");
            var trigger = TriggerBuilder.Create().WithCronSchedule(cronExpr).Build();
            addJobDataToTrigger(trigger, jobData);

            scheduler.ScheduleJob(jobDetail, trigger);
        }

        private static void ConfigureSimpleScheduleTask(IScheduler scheduler, JToken schedule, IJobDetail jobDetail, IList<Tuple<string, string>> jobData)
        {
            var settings = schedule["Settings"];
            var repeatForever = settings.Value<bool>("RepeatForever");
            var repeatEvery = settings.Value<int>("RepeatEvery");
            var repeatUnit = settings.Value<string>("RepeatUnit");
            var trigger = TriggerBuilder.Create().WithSimpleSchedule(s => {
                switch (repeatUnit.ToUpper())
                {
                    case "sec":
                        s.WithIntervalInSeconds(repeatEvery);
                    break;
                    case "min":
                        s.WithIntervalInMinutes(repeatEvery);
                    break;
                    case "hour":
                        s.WithIntervalInHours(repeatEvery);
                    break;
                    case "day":
                        s.WithIntervalInHours(repeatEvery * 24);
                    break;
                    default:
                    throw new ArgumentException($"Units '{repeatUnit}' not recognized");
                }
                if (repeatForever)
                {
                    s.RepeatForever();
                }
            }).Build();
            addJobDataToTrigger(trigger, jobData);
        }

        private static void addJobDataToTrigger(ITrigger trigger, IList<Tuple<string, string>> jobData)
        {
            foreach (var tupl in jobData)
            {
                trigger.JobDataMap.Add(tupl.Item1, tupl.Item2);
            }
        }
    }
}

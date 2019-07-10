using System;
using System.Threading.Tasks;
using Quartz;

namespace GenericScheduler.Jobs
{
    public class CmdLineJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            await Task.Run(async () => {
                Console.WriteLine("Fired at: " + context.FireTimeUtc.ToString("o"));
                await Task.Delay(50000);
                Console.WriteLine("Done");
            });
            Console.WriteLine("All done");
        }
    }
}

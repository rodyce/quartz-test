
using dotenv.net;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace quartz_test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            DotEnv.Config();
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    //config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .UseStartup<Startup>()
                .Build();
    }
}

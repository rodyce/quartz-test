using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartzmin;

namespace quartz_test
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddQuartzmin();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseQuartzmin(new QuartzminOptions() {
                Scheduler = CreateQuartzScheduler()
            });

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }

        private static readonly IEnumerable<String> CustomEnvVars = new HashSet<String> {
            "DB_HOST", "DB_USER", "DB_PASS", "DB_DATABASE", "DB_POOL_SIZE"
        };

        private IScheduler CreateQuartzScheduler()
        {
            var quartzConfigSection = _configuration.GetSection("quartz");
            var schedulerProps = new NameValueCollection();

            foreach (var setting in quartzConfigSection.GetChildren())
            {
                if (setting.Key.EndsWith(".connectionString"))
                {
                    var connectionString = setting.Value;
                    foreach (var envVar in CustomEnvVars)
                    {
                        var replaceStr = string.Format("${{{0}}}", envVar);
                        if (connectionString.Contains(replaceStr))
                        {
                            connectionString = connectionString.Replace(replaceStr, Environment.GetEnvironmentVariable(envVar));
                        }
                    }
                    schedulerProps.Add(setting.Key, connectionString);
                }
                else
                {
                    schedulerProps.Add(setting.Key, setting.Value);
                }
            }
            var customSchedulerFactory = new StdSchedulerFactory(schedulerProps);

            return customSchedulerFactory.GetScheduler().Result;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using CrystalQuartz.AspNetCore;
using CrystalQuartz.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using quartz_test.Util;
using Newtonsoft.Json;
using System.IO;
using GenericScheduler.Jobs;
using System.Threading.Tasks;
using quartz_test.Config;

namespace quartz_test
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            var scheduler = CreateScheduler();
            app.UseCrystalQuartz(
                () => scheduler,
                new CrystalQuartzOptions
                {
                    AllowedJobTypes = new[]
                    {
                        typeof(GenericScheduler.Jobs.CmdLineJob)
                    }
                });
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            ConfigureJobs(scheduler).ContinueWith((t1) =>
            {
                if (t1.IsCompletedSuccessfully)
                {
                    scheduler.Start();
                }
                else
                {
                    if (t1.Exception != null)
                    {
                        Console.WriteLine(t1.Exception.Message);
                        throw t1.Exception;
                    }
                }
            });
        }

        private IScheduler CreateScheduler()
        {
            var quartzConfigSection = Configuration.GetSection("quartz");
            var schedulerProps = new NameValueCollection();

            foreach (var setting in quartzConfigSection.GetChildren())
            {
                var settingValue = setting.Value.ReplaceEnvVariables();
                schedulerProps.Add(setting.Key, settingValue);
            }
            var customSchedulerFactory = new StdSchedulerFactory(schedulerProps);

            return customSchedulerFactory.GetScheduler().Result;
        }

        private async Task ConfigureJobs(IScheduler scheduler)
        {
            var quartzConfigSection = Configuration.GetSection("GenericScheduler");
            var configFile = string.Empty;
            foreach (var setting in quartzConfigSection.GetChildren())
            {
                switch (setting.Key)
                {
                    case "ConfigFile":
                    configFile = setting.Value;
                    break;
                }
            }

            if (configFile == string.Empty) {
                return;
            }

            TaskConfigurer.ConfigureTask(scheduler, configFile);
        }
    }
}

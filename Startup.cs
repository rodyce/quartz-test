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
            scheduler.Start();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }


        private static readonly IEnumerable<String> CustomEnvVars = new HashSet<String> {
            "DB_HOST", "DB_USER", "DB_PASS", "DB_DATABASE", "DB_POOL_SIZE"
        };

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
    }
}

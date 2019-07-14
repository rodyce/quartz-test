using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using CrystalQuartz.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;


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
            app.UseCrystalQuartz(() => scheduler);

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

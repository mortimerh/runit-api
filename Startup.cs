using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Runit.Backend.Models;

namespace Runit.Backend
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
            services.AddDbContext<RunitContext>(opt => opt.UseSqlite(Configuration["Database:SqlLite:ConnectionString"]));
            services
                .AddMvcCore(options => {
                    options.RequireHttpsPermanent = true; // this does not affect api requests
                    // options.RespectBrowserAcceptHeader = true; // false by default
                    //options.OutputFormatters.RemoveType<HttpNoContentOutputFormatter>();

                    // these two are here to show you where to include custom formatters
                    // options.OutputFormatters.Add(new CustomOutputFormatter());
                    // options.InputFormatters.Add(new CustomInputFormatter());
                })
                //.AddApiExplorer()
                .AddAuthorization()
                .AddFormatterMappings()
                //.AddCacheTagHelper()
                .AddDataAnnotations()
                //.AddCors()
                .AddJsonFormatters();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseMvc();

            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<RunitContext>();
                context.Database.EnsureCreated();
            }

        }
    }
}

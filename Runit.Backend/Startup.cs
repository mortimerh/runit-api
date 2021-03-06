﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Converters;
using Runit.Backend.Database;
using Runit.Backend.Database.Seeds;
using Runit.Backend.Infrastructure;
using Runit.Backend.Models;
using Runit.Backend.Services;

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

            services.AddIdentity<User, UserRole>()
                .AddEntityFrameworkStores<RunitContext>()
                .AddDefaultTokenProviders();

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); // => remove default claims
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

                })
                .AddJwtBearer(cfg =>
                {
                    cfg.RequireHttpsMetadata = false;
                    cfg.SaveToken = true;
                    cfg.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = Configuration["Authentication:Jwt:Issuer"],
                        ValidAudience = Configuration["Authentication:Jwt:Issuer"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Authentication:Jwt:Key"])),
                        ClockSkew = TimeSpan.Zero // remove delay of token when expire
                    };
                });
            services
            .AddCors(options =>
            {
                options.AddPolicy("CorsAllowAllPolicy", builder =>
                    builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });
            services
                .AddMvcCore(options =>
                {
                    options.RequireHttpsPermanent = false; // this does not affect api requests
                    options.ReturnHttpNotAcceptable = true;
                    options.RespectBrowserAcceptHeader = true;
                    //options.OutputFormatters.RemoveType<HttpNoContentOutputFormatter>();

                    // these two are here to show you where to include custom formatters
                    // options.OutputFormatters.Add(new CustomOutputFormatter());
                    // options.InputFormatters.Add(new CustomInputFormatter());
                    options.OutputFormatters.Add(new ICalSerializerOutputFormatter());
                })
                //.AddApiExplorer()
                .AddAuthorization()
                .AddFormatterMappings()
                //.AddCacheTagHelper()
                .AddDataAnnotations()
                .AddJsonOptions(options => {
                    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                })
                .AddJsonFormatters();

                services.AddTransient<EmailService, EmailService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseCors("CorsAllowAllPolicy"); // @fixme

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action=Index}/{id?}");
            });



            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<RunitContext>();
                var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<User>>();
                context.Database.EnsureCreated();

                if (env.IsDevelopment())
                {
                    List<Seeder> seeders = new List<Seeder>() {
                    new UserSeeder(context, userManager),
                    new ActivityTypeSeeder(context),
                    new ActivitySeeder(context),
                    new PlanSeeder(context)
                };

                    foreach (var seeder in seeders)
                    {
                        if (seeder.ShouldRun())
                        {
                            seeder.RunAsync();
                        }
                    }
                }
            }



        }
    }
}

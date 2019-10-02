using System;
using System.Text;
using System.Threading.Tasks;
using Application.Database;
using Application.Helpers;
using Application.Models;
using Application.Repositories;
using Application.Services;
using Application.Utility.Middleware;
using Application.Utility.Startup;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Prometheus;
using Application.Utility.Database;
using Application.Utility.Models;
using Polly;

namespace Application
{
    public class Startup
    {
        private readonly ILoggerFactory _loggerFactory;

        public Startup(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            Configuration = configuration;
            Logging.CreateLogger();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Set compability mode for mvc
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                });

            services.AddSingleton<IDatabaseSettings, DatabaseSettings>();
            services.AddTransient<IDatabaseContext, DatabaseContext>();
            services.AddMongoDb();
            services.AddAutoMapper(typeof(AutoMapperProfile));

            services.AddMultipleDomainSupport();

            services.GetAppSettings(Configuration);
            var appSettings = Configuration.GetSection("AppSettings").Get<AppSettings>();
            var key = Encoding.UTF8.GetBytes(appSettings.Secret);

            services.AddHttpClient<IAuthenticationService, AuthenticationService>("UserService")
                .AddTransientHttpErrorPolicy(
                    p => p.WaitAndRetryAsync(
                        3, _ => TimeSpan.FromMilliseconds(600)
                    )
                );

            services.AddAuthentication(x =>
                {
                    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(x =>
                {
                    x.Events = new JwtBearerEvents()
                    {
                        OnTokenValidated = context =>
                        {
                            var userRepository =
                                context.HttpContext.RequestServices.GetRequiredService<IAuthenticationRepository>();
                            var userId = context.Principal.Identity.Name;
                            var user = userRepository.GetById(userId);
                            if (user == null) context.Fail("Unauthorized");

                            return Task.CompletedTask;
                        }
                    };
                    x.RequireHttpsMetadata = false;
                    x.SaveToken = true;
                    x.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateAudience = true,
                        ValidAudience = "auth",
                        ValidateIssuer = false
                    };
                });
            services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddTracing(options =>
            {
                options.JaegerAgentHost = Environment.GetEnvironmentVariable("JAEGER_AGENT_HOST");
                options.ServiceName = "authentication-service";
                options.LoggerFactory = _loggerFactory;
            });

            services.AddApiDocumentation("Authentication");

            services.AddHealthChecks();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
            app.UseHttpsRedirection();

            loggerFactory.AddLogging();

            app.UseMultipleDomainSupport();
            app.UseHealthChecks("/api/health");
            app.UseMetricServer();
            app.UseRequestMiddleware();

            app.UseAuthentication();
            app.UseApiDocumentation("Authentication");

            app.UseMvc();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Application.Database;
using Application.Helpers;
using Application.Models;
using Application.Repositories;
using Application.Services;
using Application.Utility;
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
using OpenTracing;
using OpenTracing.Util;
using Polly;
using Prometheus;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace Application
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            CreateLogger();
        }

        public void CreateLogger()
        {
            try
            {
                Log.Logger = new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .MinimumLevel.Debug()
                    .WriteTo.Elasticsearch(
                        new ElasticsearchSinkOptions(new Uri(Environment.GetEnvironmentVariable("ELASTICSEARCH_URI")))
                        {
                            MinimumLogEventLevel = LogEventLevel.Verbose,
                                AutoRegisterTemplate = true
                        }).CreateLogger();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
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
            StartupDatabaseInitializer.InitializeDatabase(services);

            services.AddAutoMapper(typeof(AutoMapperProfile));

            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

            var appSettings = appSettingsSection.Get<AppSettings>();
            var key = Encoding.ASCII.GetBytes(appSettings.Secret);
            
            services.AddHttpClient<IUserService, UserService>("UserService")
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
                                context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
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
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserService, UserService>();

            //AddTracing(services);

            APIDocumentationInitializer.ApiDocumentationInitializer(services);

            services.AddHealthChecks();

            CorsConfig.AddCorsPolicy(services);
        }

        private static void AddTracing(IServiceCollection services)
        {
            services.AddSingleton<ITracer>(serviceProvider =>
            {
                var serviceName = Assembly.GetEntryAssembly().GetName().Name;

                Environment.SetEnvironmentVariable("JAEGER_SERVICE_NAME", serviceName);

                var loggerFactory = new LoggerFactory();
                try
                {
                    // add agenthost and port
                    var config = Jaeger.Configuration.FromEnv(loggerFactory);
                    var tracer = config.GetTracer();

                    GlobalTracer.Register(tracer);
                    return tracer;
                }
                catch (Exception)
                {
                    Console.WriteLine("Couldn't register logger");
                }

                return null;
            });

            // Add logging
            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
            services.AddOpenTracing();
        }

        public void ConfigureDevelopmentSevices(IServiceCollection services)
        {
            Environment.SetEnvironmentVariable("JAEGER_AGENT_HOST", "localhost");
            Environment.SetEnvironmentVariable("JAEGER_AGENT_PORT", "6831");
            Environment.SetEnvironmentVariable("JAEGER_SAMPLER_TYPE", "const");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseHealthChecks("/api/health");
            app.UseMetricServer();
            app.UseRequestMiddleware();

            app.UseAuthentication();

            APIDocumentationInitializer.AllowAPIDocumentation(app);
            CorsConfig.AddCors(app);

            app.UseMvc();
        }
    }
}
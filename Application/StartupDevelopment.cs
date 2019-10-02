using System;
using System.Text;
using System.Threading.Tasks;
using Application.Database;
using Application.Helpers;
using Application.Repositories;
using Application.Services;
using Application.Utility.Database;
using Application.Utility.Middleware;
using Application.Utility.Models;
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
using Polly;
using Prometheus;

namespace Application
{
    public class StartupDevelopment
    {
        public StartupDevelopment(IConfiguration configuration)
        {
            Configuration = configuration;
            Logging.CreateLogger();

            // ElasticSearch
            Environment.SetEnvironmentVariable("ELASTICSEARCH_URI", "http://localhost:9200");

            // Database
            Environment.SetEnvironmentVariable("CollectionName", "Models");
            Environment.SetEnvironmentVariable("ConnectionString", "mongodb://localhost:27017");
            Environment.SetEnvironmentVariable("DatabaseName", "ApplicationDb");
            Environment.SetEnvironmentVariable("MONGO_INITDB_ROOT_USERNAME", "application");
            Environment.SetEnvironmentVariable("MONGO_INITDB_ROOT_PASSWORD", "password");

            Environment.SetEnvironmentVariable("MONGO_SERVICE_NAME", "localhost");
            Environment.SetEnvironmentVariable("MONGO_SERVICE_PORT", "27017");

            Environment.SetEnvironmentVariable("JAEGER_AGENT_HOST", "localhost");
            Environment.SetEnvironmentVariable("JAEGER_AGENT_PORT", "6831");
            Environment.SetEnvironmentVariable("JAEGER_SAMPLER_TYPE", "const");
        }

        public IConfiguration Configuration { get; }

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

            services.AddHttpClient<IAuthenticationService, AuthenticationService>("UserService")
                .AddTransientHttpErrorPolicy(
                    p => p.WaitAndRetryAsync(
                        3, _ => TimeSpan.FromMilliseconds(600)
                    )
                );

            services.GetAppSettings(Configuration);
            var appSettings = Configuration.GetSection("AppSettings").Get<AppSettings>();
            var key = Encoding.UTF8.GetBytes(appSettings.Secret);

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

            services.AddApiDocumentation("Authentication");

            services.AddHealthChecks();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
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
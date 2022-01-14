using Abp.AspNetCore;
using Abp.AspNetCore.SignalR.Hubs;
using Abp.Castle.Logging.Log4Net;
using Abp.Extensions;
using Castle.Facilities.Logging;
using ElmahCore;
using ElmahCore.Mvc;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Shesha.Configuration;
using Shesha.DynamicEntities;
using Shesha.Identity;
using Shesha.Scheduler.Extensions;
using Shesha.Swagger;
using System;
using System.IO;
using System.Reflection;

namespace Shesha.Web.Host.Startup
{
    public class Startup
    {
        private readonly IConfigurationRoot _appConfiguration;
        private readonly IWebHostEnvironment _hostEnvironment;

        public Startup(IWebHostEnvironment hostEnvironment, IHostingEnvironment env)
        {
            _appConfiguration = env.GetAppConfiguration();
            _hostEnvironment = hostEnvironment;
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddElmah<XmlFileErrorLog>(options =>
            {
                options.Path = @"elmah";
                options.LogPath = Path.Combine(_hostEnvironment.ContentRootPath, "App_Data", "ElmahLogs");
                //options.CheckPermissionAction = context => context.User.Identity.IsAuthenticated; //note: looks like we have to use cookies for it
            });

            services.AddMvcCore(options =>
                {
                    options.SuppressInputFormatterBuffering = true;

                    options.EnableEndpointRouting = false;
                    options.Conventions.Add(new ApiExplorerGroupPerVersionConvention());

                    options.EnableDynamicDtoBinding();
                })
                .AddApiExplorer()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.DateParseHandling = DateParseHandling.DateTimeOffset;
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            IdentityRegistrar.Register(services);
            AuthConfigurer.Configure(services, _appConfiguration);

            services.AddSignalR();

            services.AddCors();

            // Swagger - Enable this line and the related lines in Configure method to enable swagger UI
            services.AddSwaggerGen(options =>
            {
                options.DescribeAllParametersInCamelCase();
                options.IgnoreObsoleteActions();
                options.AddXmlDocuments();

                options.OperationFilter<SwaggerOperationFilter>();
                options.OperationFilter<SwaggerDefaultValues>();

                options.CustomOperationIds(desc => desc.ActionDescriptor is ControllerActionDescriptor d 
                    ? d.ControllerName.ToCamelCase() + d.ActionName.ToPascalCase()
                    : null);
                options.SwaggerDoc("v1", new OpenApiInfo() { Title = "Shesha API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);

                options.AddDocumentsPerService();

                // Define the BearerAuth scheme that's in use
                options.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme()
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });
            });

            services.AddHttpContextAccessor();
            services.AddHangfire(config =>
            {
                config.UseSqlServerStorage(_appConfiguration.GetConnectionString("Default"));
            });

            // Add ABP and initialize 
            // Configure Abp and Dependency Injection
            return services.AddAbp<SheshaWebHostModule>(
                options =>
                {
                    // Configure Log4Net logging
                    options.IocManager.IocContainer.AddFacility<LoggingFacility>(f => f.UseAbpLog4Net().WithConfig("log4net.config"));
                    // configure plugins
                    //options.PlugInSources.AddFolder(Path.Combine(_hostingEnvironment.WebRootPath, "Plugins"), SearchOption.AllDirectories);
                }
            );
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseElmah();

            // note: already registered in the ABP
            AppContextHelper.Configure(app.ApplicationServices.GetRequiredService<IHttpContextAccessor>());

            // use NHibernate session per request
            //app.UseNHibernateSessionPerRequest();

            app.UseHangfireServer();

            app.UseAbp(options => { options.UseAbpRequestLocalization = false; }); // Initializes ABP framework.

            // global cors policy
            app.UseCors(x => x
                .AllowAnyMethod()
                .AllowAnyHeader()
                .SetIsOriginAllowed(origin => true) // allow any origin
                .AllowCredentials()); // allow credentials

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseAbpRequestLocalization();

            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "defaultWithArea",
                    pattern: "{area}/{controller=Home}/{action=Index}/{id?}");
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapHub<AbpCommonHub>("/signalr");
                endpoints.MapControllers();
                endpoints.MapSignalRHubs();
            });

            // Enable middleware to serve generated Swagger as a JSON endpoint
            app.UseSwagger();

            // Enable middleware to serve swagger-ui assets (HTML, JS, CSS etc.)
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("swagger/v1/swagger.json", "Shesha API V1");
                options.IndexStream = () => Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("Shesha.Web.Host.wwwroot.swagger.ui.index.html");
            }); // URL: /swagger

            app.UseHangfireDashboard();
        }
    }
}

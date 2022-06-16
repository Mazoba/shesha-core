using Abp.AspNetCore;
using Abp.AspNetCore.SignalR.Hubs;
using Abp.Castle.Logging.Log4Net;
using Abp.Extensions;
using Castle.Facilities.Logging;
using ElmahCore;
using ElmahCore.Mvc;
using GraphQL;
using GraphQL.MicrosoftDI;
using GraphQL.NewtonsoftJson;
using GraphQL.Server;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.SystemTextJson;
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
using Shesha.Authorization;
using Shesha.Configuration;
using Shesha.DynamicEntities;
using Shesha.DynamicEntities.Swagger;
using Shesha.GraphQL;
using Shesha.Identity;
using Shesha.Scheduler.Extensions;
using Shesha.Swagger;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using Shesha.GraphQL.Middleware;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shesha.GraphQL.Provider.Queries;
using GraphQL.Server;
using Shesha.GraphQL.Provider.GraphTypes;
using Abp.Application.Services.Dto;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;

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
            services.Configure<IISServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

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
                    options.AddDynamicAppServices(services);
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

            AddApiVersioning(services);

            services.AddHttpContextAccessor();
            services.AddHangfire(config =>
            {
                config.UseSqlServerStorage(_appConfiguration.GetConnectionString("Default"));
            });

            // ToDo: fix AbpAuthorizationFilter
            services.AddMvc(options =>
            {
                options.Filters.AddService(typeof(SheshaAuthorizationFilter));
            });

            //services.AddScoped<SheshaSchema>();

            // Add GraphQL services and configure options
            services.AddGraphQL(builder => builder
                //.AddHttpMiddleware<SheshaSchema, GraphQLHttpMiddleware<SheshaSchema>>()
                //.AddHttpMiddleware<EmptySchema, GraphQLHttpMiddleware<EmptySchema>>()
                //.AddWebSocketsHttpMiddleware<SheshaSchema>()
                // For subscriptions support
                //.AddDocumentExecuter<SubscriptionDocumentExecuter>()
                //.AddSchema<SheshaSchema>()
                //.AddSchema<EmptySchema>()
                .ConfigureExecutionOptions(options =>
                {
                    options.EnableMetrics = true;// Environment.IsDevelopment();
                    var logger = options.RequestServices.GetRequiredService<ILogger<Startup>>();

                    var unitOfWorkManager = options.RequestServices.GetRequiredService<IUnitOfWorkManager>();
                    //options.Query.
                    options.Listeners.Add(new GraphQLNhListener(unitOfWorkManager));

                    options.UnhandledExceptionDelegate = ctx =>
                    {
                        logger.LogError("{Error} occurred", ctx.OriginalException.Message);
                        return Task.CompletedTask;
                    };
                })
                // Add required services for GraphQL request/response de/serialization
                .AddSystemTextJson() // For .NET Core 3+
                //.AddNewtonsoftJson() // For everything else
                //.AddErrorInfoProvider(opt => opt.ExposeExceptionStackTrace = Environment.IsDevelopment())
                .AddWebSockets() // Add required services for web socket support
                //.AddDataLoader() // Add required services for DataLoader support
                .AddGraphTypes(typeof(SheshaSchema).Assembly)
                ); // Add all IGraphType implementors in assembly which ChatSchema exists 
                                                                //.AddGraphTypes(ServiceLifetime.Scoped)
                                                                //.AddUserContextBuilder(httpContext => httpContext.User)
                                                                //.AddDataLoader();

            services.AddSingleton<GraphQLMiddleware>();
            services.AddSingleton(new GraphQLSettings
            {
                BuildUserContext = ctx => new GraphQLUserContext
                {
                    User = ctx.User
                },
                EnableMetrics = true
            });
            services.TryAddTransient(typeof(EntityQuery<,>));
            services.TryAddTransient(typeof(GraphQLGenericType<>));
            services.TryAddTransient(typeof(PagedResultDtoType<>));
            services.TryAddTransient(typeof(PagedAndSortedResultRequestDto));
            services.TryAddTransient(typeof(GraphQLInputGenericType<>));


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

            app.UseMiddleware<GraphQLMiddleware>();
            /*
            app.UseGraphQL<SheshaSchema>(path: "/graphql/person");
            app.UseGraphQL<EmptySchema>(path: "/graphql/empty");
            */
            app.UseGraphQLPlayground(); //to explorer API navigate https://*DOMAIN*/ui/playground
        }

        private void AddApiVersioning(IServiceCollection services)
        {
            services.Replace(ServiceDescriptor.Singleton<IApiControllerSpecification, AbpAppServiceApiVersionSpecification>());
            services.Configure<OpenApiInfo>(_appConfiguration.GetSection(nameof(OpenApiInfo)));

            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

            //Swagger - Enable this line and the related lines in Configure method to enable swagger UI
            services.AddSwaggerGen(options =>
            {
                options.DescribeAllParametersInCamelCase();
                options.IgnoreObsoleteActions();
                options.AddXmlDocuments();

                options.SchemaFilter<DynamicDtoSchemaFilter>();

                options.CustomSchemaIds(type => SwaggerHelper.GetSchemaId(type));

                options.CustomOperationIds(desc => desc.ActionDescriptor is ControllerActionDescriptor d
                    ? d.ControllerName.ToCamelCase() + d.ActionName.ToPascalCase()
                    : null);

                options.AddDocumentsPerService();

                // Define the BearerAuth scheme that's in use
                options.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme()
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });
                //options.SchemaFilter<DynamicDtoSchemaFilter>();
            });
            services.Replace(ServiceDescriptor.Transient<ISwaggerProvider, CachingSwaggerProvider>());

            services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = ApiVersion.Default;
                options.ReportApiVersions = true;
            });

            services.AddVersionedApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });
        }
    }
}

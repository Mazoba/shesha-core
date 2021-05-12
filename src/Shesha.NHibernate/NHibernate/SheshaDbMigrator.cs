using System;
using System.Configuration;
using Abp.Dependency;
using Abp.Extensions;
using Abp.MultiTenancy;
using Abp.Reflection;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;

namespace Shesha.NHibernate
{
    public class SheshaDbMigrator : IAbpZeroDbMigrator, ITransientDependency
    {
        private readonly IAssemblyFinder _assemblyFinder;
        private readonly IDbPerTenantConnectionStringResolver _connectionStringResolver;

        public SheshaDbMigrator(IDbPerTenantConnectionStringResolver connectionStringResolver, IAssemblyFinder assemblyFinder)
        {
            _connectionStringResolver = connectionStringResolver;
            _assemblyFinder = assemblyFinder;
        }

        public virtual void CreateOrMigrateForHost()
        {
            CreateOrMigrateForHost(null);
        }

        public virtual void CreateOrMigrateForHost(Action seedAction)
        {
            CreateOrMigrate(null, seedAction);
        }


        public virtual void CreateOrMigrateForTenant(AbpTenantBase tenant)
        {
            CreateOrMigrateForTenant(tenant, null);
        }

        public virtual void CreateOrMigrateForTenant(AbpTenantBase tenant, Action seedAction)
        {
            if (tenant.ConnectionString.IsNullOrEmpty())
                return;

            CreateOrMigrate(tenant, seedAction);
        }

        /// <summary>
        /// Configure the dependency injection services
        /// </summary>
        private IServiceProvider CreateServices(string connectionString)
        {
            return new ServiceCollection()
                // Add common FluentMigrator services
                .AddFluentMigratorCore()
                .ConfigureRunner(rb =>
                    {
                        rb.WithGlobalCommandTimeout(TimeSpan.FromMinutes(30));

                        rb.AddSqlServer2012()
                            // Set the connection string
                            .WithGlobalConnectionString(connectionString);
                        
                        var assemblies = _assemblyFinder.GetAllAssemblies();
                        foreach (var assembly in assemblies)
                        {
                            // Define the assembly containing the migrations
                            rb.ScanIn(assembly).For.Migrations();
                        }
                    }
                )

                // Enable logging to console in the FluentMigrator way
                .AddLogging(lb => lb.AddFluentMigratorConsole())
                // Build the service provider
                .BuildServiceProvider(false);
        }

        /// <summary>
        /// Update the database
        /// </summary>
        private void CreateOrMigrate(AbpTenantBase tenant, Action seedAction)
        {
            var args = new DbPerTenantConnectionStringResolveArgs(
                tenant == null ? (int?)null : (int?)tenant.Id,
                tenant == null ? MultiTenancySides.Host : MultiTenancySides.Tenant
            );

            //args["DbContextType"] = typeof(TDbContext);
            //args["DbContextConcreteType"] = typeof(TDbContext);

            var connectionString = GetConnectionString(
                _connectionStringResolver.GetNameOrConnectionString(args)
            );

            // Put the database update into a scope to ensure
            // that all resources will be disposed.
            using var scope = CreateServices(connectionString).CreateScope();

            // Instantiate the runner
            var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

            try
            {
                // Execute the migrations
                runner.MigrateUp();
            }
            catch (Exception e)
            {
                 throw;
            }
        }

        /// <summary>
        /// Gets connection string from given connection string or name.
        /// </summary>
        private static string GetConnectionString(string nameOrConnectionString)
        {
            var connStrSection = ConfigurationManager.ConnectionStrings[nameOrConnectionString];
            if (connStrSection != null)
            {
                return connStrSection.ConnectionString;
            }

            return nameOrConnectionString;
        }
    }
}

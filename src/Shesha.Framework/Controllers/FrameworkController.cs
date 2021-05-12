using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Configuration;
using Abp.Dependency;
using Abp.Reflection;
using Abp.Web.Models;
using Castle.Core.Logging;
using Microsoft.AspNetCore.Mvc;
using NHibernate;
using Shesha.Bootstrappers;
using Shesha.Configuration;
using Shesha.Domain.Attributes;
using Shesha.Extensions;
using Shesha.Migrations;
using Shesha.Reflection;
using Shesha.Services;

namespace Shesha.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class FrameworkController: ControllerBase, ITransientDependency
    {
        private readonly ISettingManager _settingManager;
        public ILogger Logger { get; set; } = new NullLogger();

        public FrameworkController(ISettingManager settingManager)
        {
            _settingManager = settingManager;
        }

        [HttpGet]
        [DontWrapResult]
        public IList ExecuteHql(string query)
        {
            var sessionFactory = StaticContext.IocManager.Resolve<ISessionFactory>();
            var session = sessionFactory.GetCurrentSession();
            var list = session.CreateQuery(query).List();
            return list;
        }

        /// <summary>
        /// NOTE: to be removed
        /// </summary>
        [HttpGet]
        [DontWrapResult]
        public string TestEntities()
        {
            try
            {
                var typeFinder = StaticContext.IocManager.Resolve<ITypeFinder>();
                var sessionFactory = StaticContext.IocManager.Resolve<ISessionFactory>();
                var migrationGenerator = StaticContext.IocManager.Resolve<IMigrationGenerator>();

                var types = typeFinder.FindAll().Where(t => t.IsEntityType()).ToList();

                var errors = new Dictionary<Type, Exception>();

                var session = sessionFactory.GetCurrentSession();

                foreach (var type in types)
                {
                    try
                    {
                        var hql = $"from {type.FullName}";
                        var list = session.CreateQuery(hql).SetMaxResults(1).List();
                    }
                    catch (Exception e)
                    {
                        errors.Add(type, e);
                    }
                }

                var typesToMap = errors.Select(e => e.Key).Where(t => !t.Namespace.StartsWith("Abp") && !t.HasAttribute<ImMutableAttribute>()).ToList();

                var migration = migrationGenerator.GenerateMigrations(typesToMap);

                var grupped = migrationGenerator.GroupByPrefixes(typesToMap);
                var grouppedMigrations = grupped.Select(g => new { Prefix = g.Key, Migration = migrationGenerator.GenerateMigrations(g.Value) })
                    .ToList();

                return migration;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        [HttpPost]
        public async Task<string> BootstrapReferenceLists()
        {
            var bootstrapper = StaticContext.IocManager.Resolve<ReferenceListBootstrapper>();
            await bootstrapper.Process();
            return "Bootstrapped successfully";
        }
    }
}
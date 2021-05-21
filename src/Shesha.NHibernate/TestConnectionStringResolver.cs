﻿using Abp.Domain.Uow;
using Abp.MultiTenancy;
using Shesha.NHibernate;

namespace Shesha
{
    /// <summary>
    /// Resolves connection string for unit tests
    /// </summary>
    public class TestConnectionStringResolver : IDbPerTenantConnectionStringResolver
    {
        private readonly SheshaNHibernateModule _nhModule;

        public TestConnectionStringResolver(SheshaNHibernateModule nhModule)
        {
            _nhModule = nhModule;
        }

        public string GetNameOrConnectionString(ConnectionStringResolveArgs args)
        {
            return _nhModule.ConnectionString;
        }

        public string GetNameOrConnectionString(DbPerTenantConnectionStringResolveArgs args)
        {
            return _nhModule.ConnectionString;
        }
    }
}
﻿using Abp.Dependency;
using Abp.Linq;
using NHibernate.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shesha.NHibernate
{
    /// <summary>
    /// Nhibernate queryable async executer. Is used to abstract from NHibernate dependencies
    /// </summary>
    public class NhAsyncQueryableExecuter : IAsyncQueryableExecuter, ISingletonDependency
    {
        public Task<int> CountAsync<T>(IQueryable<T> queryable)
        {
            return queryable.CountAsync();
        }

        public Task<List<T>> ToListAsync<T>(IQueryable<T> queryable)
        {
            return queryable.ToListAsync();
        }

        public Task<T> FirstOrDefaultAsync<T>(IQueryable<T> queryable)
        {
            return queryable.FirstOrDefaultAsync();
        }

        public Task<bool> AnyAsync<T>(IQueryable<T> queryable)
        {
            return queryable.AnyAsync();
        }
    }
}

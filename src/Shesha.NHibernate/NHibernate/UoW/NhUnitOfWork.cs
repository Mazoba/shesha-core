using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.EntityHistory;
using Abp.Runtime.Session;
using Abp.Transactions.Extensions;
using NHibernate;

namespace Shesha.NHibernate.UoW
{
    /// <summary>
    /// Implements Unit of work for NHibernate.
    /// </summary>
    public class NhUnitOfWork : UnitOfWorkBase, ITransientDependency
    {
        /// <summary>
        /// Gets NHibernate session object to perform queries.
        /// </summary>
        public ISession Session { get; private set; }

        /// <summary>
        /// <see cref="NhUnitOfWork"/> uses this DbConnection if it's set.
        /// This is usually set in tests.
        /// </summary>
        public DbConnection DbConnection { get; set; }

        /// <summary>
        /// Entity history helper
        /// </summary>
        public EntityHistoryHelperBase EntityHistoryHelper { get; set; }

        private readonly ISessionFactory _sessionFactory;
        private ITransaction _transaction;

        private readonly SheshaNHibernateModule _nhModule;

        /// <summary>
        /// Creates a new instance of <see cref="NhUnitOfWork"/>.
        /// </summary>
        [DebuggerStepThrough]
        public NhUnitOfWork(
            ISessionFactory sessionFactory,
            IConnectionStringResolver connectionStringResolver,
            IUnitOfWorkDefaultOptions defaultOptions,
            IUnitOfWorkFilterExecuter filterExecuter,

            SheshaNHibernateModule nhModule)
            : base(
                  connectionStringResolver,
                  defaultOptions,
                  filterExecuter)
        {
            _sessionFactory = sessionFactory;
            
            _nhModule = nhModule;
        }

        protected override void BeginUow()
        {
            //EntityHistoryHelper = StaticContext.IocManager.Resolve<EntityHistoryHelperBase>();

            Session = DbConnection != null
                ? _sessionFactory.WithOptions().Connection(DbConnection).OpenSession()
                : _sessionFactory.OpenSession();

            #region temporary stuff for unit testing

            //var nhModule = StaticContext.IocManager.Resolve<AbpNHibernateModule>();
            var nhModule = _nhModule;
            /*
            if (nhModule.UseInMemoryDatabase)
            {
                var export = new SchemaExport(nhModule.NhConfiguration);
                try
                {
                    export.Execute(true, true, false, Session.Connection, null);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
            */
            #endregion

            Session.FlushMode = FlushMode.Commit;

            if (Options.IsTransactional == true)
            {
                _transaction = Options.IsolationLevel.HasValue
                    ? Session.BeginTransaction(Options.IsolationLevel.Value.ToSystemDataIsolationLevel())
                    : Session.BeginTransaction();
            }

            // set default value for IsDeleted parameter
            SetFilterParameter(AbpDataFilters.SoftDelete, AbpDataFilters.Parameters.IsDeleted, false);

            CheckAndSetSoftDelete();
            CheckAndSetMayHaveTenant();
            CheckAndSetMustHaveTenant();
        }

        protected virtual void CheckAndSetSoftDelete()
        {
            if (IsFilterEnabled(AbpDataFilters.SoftDelete))
            {
                ApplyEnableFilter(AbpDataFilters.SoftDelete); //Enable Filters
                ApplyFilterParameterValue(AbpDataFilters.SoftDelete, AbpDataFilters.Parameters.IsDeleted, false); //ApplyFilter
            }
            else
            {
                ApplyDisableFilter(AbpDataFilters.SoftDelete); //Disable filters
            }
        }

        protected virtual void CheckAndSetMustHaveTenant()
        {
            if (IsFilterEnabled(AbpDataFilters.MustHaveTenant))
            {
                ApplyEnableFilter(AbpDataFilters.MustHaveTenant); //Enable Filters
                ApplyFilterParameterValue(AbpDataFilters.MustHaveTenant, AbpDataFilters.Parameters.TenantId, AbpSession.GetTenantId()); //ApplyFilter
            }
            else
            {
                ApplyDisableFilter(AbpDataFilters.MustHaveTenant); //Disable Filters
            }
        }

        protected virtual void CheckAndSetMayHaveTenant()
        {
            if (IsFilterEnabled(AbpDataFilters.MayHaveTenant))
            {
                ApplyEnableFilter(AbpDataFilters.MayHaveTenant); //Enable Filters
                ApplyFilterParameterValue(AbpDataFilters.MayHaveTenant, AbpDataFilters.Parameters.TenantId, AbpSession.TenantId); //ApplyFilter
            }
            else
            {
                ApplyDisableFilter(AbpDataFilters.MayHaveTenant); //Disable Filters
            }

        }

        public override void SaveChanges()
        {
            Session.Flush();
        }

        public override Task SaveChangesAsync()
        {
            return Session.FlushAsync();
        }

        /// <summary>
        /// Commits transaction and closes database connection.
        /// </summary>
        protected override void CompleteUow()
        {
            SaveChanges();
            if (_transaction != null && _transaction.IsActive)
            {
                _transaction.Commit();
            }
        }

        protected override async Task CompleteUowAsync()
        {
            await SaveChangesAsync();
            if (_transaction != null && _transaction.IsActive)
            {
                try
                {
                    await _transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        /// <summary>
        /// Rollbacks transaction and closes database connection.
        /// </summary>
        protected override void DisposeUow()
        {
            if (_transaction != null)
            {
                _transaction.Dispose();
                _transaction = null;
            }

            Session.Dispose();
        }
    }
}
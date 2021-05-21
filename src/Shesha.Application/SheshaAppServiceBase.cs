using System;
using System.Globalization;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.IdentityFramework;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using NHibernate.Linq;
using Shesha.Authorization.Users;
using Shesha.MultiTenancy;
using Shesha.Services;

namespace Shesha
{
    /// <summary>
    /// Derive your application services from this class.
    /// </summary>
    public abstract class SheshaAppServiceBase : ApplicationService
    {
        /// <summary>
        /// Reference to the IoC manager.
        /// </summary>
        public IIocManager IocManager { get; set; }

        /// <summary>
        /// Dynamic repository
        /// </summary>
        public IDynamicRepository DynamicRepo { protected get; set; }

        /// <summary>
        /// Tenant manager
        /// </summary>
        public TenantManager TenantManager { get; set; }

        /// <summary>
        /// User Manager
        /// </summary>
        public UserManager UserManager { get; set; }


        private IUrlHelper _url;

        /// <summary>
        /// Url helper
        /// </summary>
        public IUrlHelper Url
        {
            get
            {
                if (_url == null)
                {
                    var actionContextAccessor = IocManager.Resolve<IActionContextAccessor>();
                    var urlHelperFactory = IocManager.Resolve<IUrlHelperFactory>();
                    _url = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
                }

                return _url;
            }
        }

        /// <summary>
        /// Cet current logged in person
        /// </summary>
        /// <returns></returns>
        protected virtual async Task<Domain.Person> GetCurrentPersonAsync()
        {
            var personRepository = IocManager.Resolve<IRepository<Domain.Person, Guid>>();
            var person = await personRepository.GetAll().FirstOrDefaultAsync(p => p.User.Id == AbpSession.GetUserId());
            return person;
        }

        /// <summary>
        /// Get current logged in user
        /// </summary>
        /// <returns></returns>
        protected virtual async Task<User> GetCurrentUserAsync()
        {
            var user = await UserManager.FindByIdAsync(AbpSession.GetUserId().ToString());
            if (user == null)
            {
                throw new Exception("There is no current user!");
            }

            return user;
        }

        /// <summary>
        /// Get current tenant
        /// </summary>
        /// <returns></returns>
        protected virtual Task<Tenant> GetCurrentTenantAsync()
        {
            return TenantManager.GetByIdAsync(AbpSession.GetTenantId());
        }

        /// <summary>
        /// Check errors
        /// </summary>
        /// <param name="identityResult"></param>
        protected virtual void CheckErrors(IdentityResult identityResult)
        {
            identityResult.CheckErrors(LocalizationManager);
        }

        /// <summary>
        /// Saves or update entity with the specified id
        /// </summary>
        /// <typeparam name="T">Type of entity</typeparam>
        /// <param name="id">Id of the existing entity or null for a new one</param>
        /// <param name="action">Update action</param>
        protected async Task<T> SaveOrUpdateEntityAsync<T>(Guid? id, Func<T, Task> action)
            where T : class, IEntity<Guid>
        {
            return await SaveOrUpdateEntityAsync<T, Guid>(id, action);
        }

        /// <summary>
        /// Saves or update entity with the specified id
        /// </summary>
        /// <typeparam name="T">Type of entity</typeparam>
        /// <typeparam name="TId">Id type</typeparam>
        /// <param name="id">Id of the existing entity or null for a new one</param>
        /// <param name="action">Update action</param>
        /// <returns></returns>
        protected async Task<T> SaveOrUpdateEntityAsync<T, TId>(TId? id, Func<T, Task> action) where T : class, IEntity<TId> where TId: struct
        {
            var item = id.HasValue 
                ? await GetEntityAsync<T, TId>(id.Value) 
                : (T)Activator.CreateInstance(typeof(T));

            await action.Invoke(item);

            await DynamicRepo.SaveOrUpdateAsync(item);

            return item;
        }

        /// <summary>
        /// Returns entity of the specified type with the specified <paramref name="id"/>
        /// </summary>
        /// <typeparam name="T">Type of entity</typeparam>
        /// <param name="id">Id of the entity</param>
        /// <param name="throwException">Throw exception if entity not found</param>
        /// <returns></returns>
        protected async Task<T> GetEntityAsync<T>(Guid id, bool throwException = true) where T : class, IEntity<Guid>
        {
            return await GetEntityAsync<T, Guid>(id, throwException);
        }

        /// <summary>
        /// Returns entity of the specified type with the specified <paramref name="id"/>
        /// </summary>
        /// <typeparam name="T">Type of entity</typeparam>
        /// <typeparam name="TId">Id type</typeparam>
        /// <param name="id">Id of the entity</param>
        /// <param name="throwException">Throw exception if entity not found</param>
        /// <returns></returns>
        protected async Task<T> GetEntityAsync<T, TId>(TId id, bool throwException = true) where T : class, IEntity<TId>
        {
            var item = await DynamicRepo.GetAsync(typeof(T), id.ToString());

            if (item != null)
                return (T)item;

            if (throwException)
                throw new UserFriendlyException($"{typeof(T).Name} with the specified id `{id}` not found");

            return null;
        }

        #region Settings

        /// <summary>
        /// Changes setting for tenant with fallback to application
        /// </summary>
        /// <param name="name">Setting name</param>
        /// <param name="value">Setting value</param>
        protected async Task ChangeSettingAsync(string name, string value)
        {
            if (AbpSession.TenantId.HasValue)
            {
                await SettingManager.ChangeSettingForTenantAsync(AbpSession.TenantId.Value, name, value);
            }
            else
            {
                await SettingManager.ChangeSettingForApplicationAsync(name, value);
            }
        }

        /// <summary>
        /// Changes setting for tenant with fallback to application
        /// </summary>
        /// <param name="name">Setting name</param>
        /// <param name="value">Setting value</param>
        protected async Task ChangeSettingAsync<T>(string name, T value) where T : struct, IConvertible
        {
            await ChangeSettingAsync(name, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Changes setting for tenant with fallback to application
        /// </summary>
        /// <param name="name">Setting name</param>
        /// <param name="value">Setting value</param>
        protected async Task<string> GetSettingValueAsync(string name)
        {
            if (AbpSession.TenantId.HasValue)
            {
                return await SettingManager.GetSettingValueForTenantAsync(name, AbpSession.TenantId.Value);
            }
            else
            {
                return await SettingManager.GetSettingValueForApplicationAsync(name);
            }
        }

        #endregion
    }
}

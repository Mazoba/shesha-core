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
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using NHibernate.Linq;
using Shesha.Authorization.Users;
using Shesha.DynamicEntities;
using Shesha.DynamicEntities.Dtos;
using Shesha.MultiTenancy;
using Shesha.ObjectMapper;
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

        /// <summary>
        /// Dynamic DTO builder
        /// </summary>
        public IDynamicDtoTypeBuilder DtoBuilder { get; set; }

        /// <summary>
        /// Dynamic property manager
        /// </summary>
        public IDynamicPropertyManager DynamicPropertyManager { get; set; }

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

        #region Dynamic DTOs

        /// <summary>
        /// Map entity to a <see cref="DynamicDto{TEntity, TPrimaryKey}"/>
        /// </summary>
        /// <typeparam name="TEntity">Type of entity</typeparam>
        /// <typeparam name="TPrimaryKey">Type of entity primary key</typeparam>
        /// <param name="entity">entity to map</param>
        /// <returns></returns>
        protected async Task<DynamicDto<TEntity, TPrimaryKey>> MapToDynamicDtoAsync<TEntity, TPrimaryKey>(TEntity entity) where TEntity : class, IEntity<TPrimaryKey>
        {
            return await MapToCustomDynamicDtoAsync<DynamicDto<TEntity, TPrimaryKey>, TEntity, TPrimaryKey>(entity);
        }

        /// <summary>
        /// Map entity to a custom <see cref="DynamicDto{TEntity, TPrimaryKey}"/>
        /// </summary>
        /// <typeparam name="TDynamicDto">Type of dynamic DTO</typeparam>
        /// <typeparam name="TEntity">Type of entity</typeparam>
        /// <typeparam name="TPrimaryKey">Type of entity primary key</typeparam>
        /// <param name="entity">entity to map</param>
        /// <returns></returns>
        protected async Task<TDynamicDto> MapToCustomDynamicDtoAsync<TDynamicDto, TEntity, TPrimaryKey>(TEntity entity) 
            where TEntity : class, IEntity<TPrimaryKey>
            where TDynamicDto : class, IDynamicDto<TEntity, TPrimaryKey>
        {
            // build dto type
            var context = new DynamicDtoTypeBuildingContext() { 
                ModelType = typeof(TDynamicDto),
            };
            var dtoType = await DtoBuilder.BuildDtoFullProxyTypeAsync(typeof(TDynamicDto), context);
            var dto = Activator.CreateInstance(dtoType) as TDynamicDto;

            // create mapper
            var mapper = GetMapper(typeof(TEntity), dtoType);

            // map entity to DTO
            mapper.Map(entity, dto);
            // map dynamic fields
            await DynamicPropertyManager.MapEntityToDtoAsync<TPrimaryKey, TDynamicDto, TEntity>(entity, dto);

            return dto;
        }

        private IMapper GetMapper(Type sourceType, Type destinationType)
        {
            var modelConfigMapperConfig = new MapperConfiguration(cfg =>
            {
                var mapExpression = cfg.CreateMap(sourceType, destinationType);

                // todo: move to conventions
                //cfg.CreateMap<RefListPersonTitle, Int64>().ConvertUsing<EnumToInt64TypeConverter<RefListPersonTitle>>();
                //cfg.CreateMap<Int64, RefListPersonTitle>().ConvertUsing<Int64ToEnumTypeConverter<RefListPersonTitle>>();

                var entityMapProfile = IocManager.Resolve<EntityMapProfile>();
                cfg.AddProfile(entityMapProfile);
            });

            return modelConfigMapperConfig.CreateMapper();
        }

        #endregion
    }
}

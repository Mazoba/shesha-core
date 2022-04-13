﻿using Abp.Application.Services;
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
using Shesha.DynamicEntities.Mapper;
using Shesha.MultiTenancy;
using Shesha.Services;
using System;
using System.Globalization;
using System.Threading.Tasks;

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

        /// <summary>
        /// Dynamic DTO mapping helper
        /// </summary>
        public IDynamicDtoMappingHelper DynamicDtoMappingHelper { get; set; }

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
        /// <param name="settings">mapping settings</param>
        /// <returns></returns>
        protected async Task<DynamicDto<TEntity, TPrimaryKey>> MapToDynamicDtoAsync<TEntity, TPrimaryKey>(TEntity entity, IDynamicMappingSettings settings = null) where TEntity : class, IEntity<TPrimaryKey>
        {
            return await MapToCustomDynamicDtoAsync<DynamicDto<TEntity, TPrimaryKey>, TEntity, TPrimaryKey>(entity, settings);
        }

        /// <summary>
        /// Map entity to a custom <see cref="DynamicDto{TEntity, TPrimaryKey}"/>
        /// </summary>
        /// <typeparam name="TDynamicDto">Type of dynamic DTO</typeparam>
        /// <typeparam name="TEntity">Type of entity</typeparam>
        /// <typeparam name="TPrimaryKey">Type of entity primary key</typeparam>
        /// <param name="entity">entity to map</param>
        /// <param name="settings">mapping settings</param>
        /// <returns></returns>
        protected async Task<TDynamicDto> MapToCustomDynamicDtoAsync<TDynamicDto, TEntity, TPrimaryKey>(TEntity entity, IDynamicMappingSettings settings = null) 
            where TEntity : class, IEntity<TPrimaryKey>
            where TDynamicDto : class, IDynamicDto<TEntity, TPrimaryKey>
        {
            // build dto type
            var context = new DynamicDtoTypeBuildingContext() { 
                ModelType = typeof(TDynamicDto),
                UseDtoForEntityReferences = settings?.UseDtoForEntityReferences ?? false
            };
            var dtoType = await DtoBuilder.BuildDtoFullProxyTypeAsync(typeof(TDynamicDto), context);
            var dto = Activator.CreateInstance(dtoType) as TDynamicDto;

            // create mapper
            var mapper = await DynamicDtoMappingHelper.GetEntityToDtoMapperAsync(typeof(TEntity), dtoType);

            // map entity to DTO
            mapper.Map(entity, dto);
            // map dynamic fields
            await DynamicPropertyManager.MapEntityToDtoAsync<TDynamicDto, TEntity, TPrimaryKey>(entity, dto);

            return dto;
        }

        /// <summary>
        /// Map static properties (defined in the entity class) of dynamic DTO to a specified entity
        /// </summary>
        /// <typeparam name="TDynamicDto">Type of Dynamic DTO</typeparam>
        /// <typeparam name="TEntity">Type of entity</typeparam>
        /// <typeparam name="TPrimaryKey">Type of primary key</typeparam>
        /// <param name="dto">Source DTO</param>
        /// <param name="entity">Destination entity</param>
        /// <returns></returns>
        protected async Task MapStaticPropertiesToEntityDtoAsync<TDynamicDto, TEntity, TPrimaryKey>(TDynamicDto dto, TEntity entity)
            where TEntity : class, IEntity<TPrimaryKey>
            where TDynamicDto : class, IDynamicDto<TEntity, TPrimaryKey>
        {
            var mapper = await DynamicDtoMappingHelper.GetDtoToEntityMapperAsync(entity.GetType(), dto.GetType());

            mapper.Map(dto, entity);
        }

        /// <summary>
        /// Map dynamic properties (defined using entity configurator) of dynamic DTO to a specified entity
        /// </summary>
        /// <typeparam name="TDynamicDto">Type of Dynamic DTO</typeparam>
        /// <typeparam name="TEntity">Type of entity</typeparam>
        /// <typeparam name="TPrimaryKey">Type of primary key</typeparam>
        /// <param name="dto">Source DTO</param>
        /// <param name="entity">Destination entity</param>
        /// <returns></returns>
        protected async Task MapDynamicPropertiesToEntityAsync<TDynamicDto, TEntity, TPrimaryKey>(TDynamicDto dto, TEntity entity)
            where TEntity : class, IEntity<TPrimaryKey>
            where TDynamicDto : class, IDynamicDto<TEntity, TPrimaryKey>
        {
            await DynamicPropertyManager.MapDtoToEntityAsync<TDynamicDto, TEntity, TPrimaryKey>(dto, entity);
        }

        /// <summary>
        /// Map all properties (dynamic and static) of dynamic DTO to a specified entity
        /// </summary>
        /// <typeparam name="TDynamicDto">Type of Dynamic DTO</typeparam>
        /// <typeparam name="TEntity">Type of entity</typeparam>
        /// <typeparam name="TPrimaryKey">Type of primary key</typeparam>
        /// <param name="dto">Source DTO</param>
        /// <param name="entity">Destination entity</param>
        /// <returns></returns>
        protected async Task MapDynamicDtoToEntityAsync<TDynamicDto, TEntity, TPrimaryKey>(TDynamicDto dto, TEntity entity)
            where TEntity : class, IEntity<TPrimaryKey>
            where TDynamicDto : class, IDynamicDto<TEntity, TPrimaryKey>
        {
            await MapStaticPropertiesToEntityDtoAsync<TDynamicDto, TEntity, TPrimaryKey>(dto, entity);
            await MapDynamicPropertiesToEntityAsync<TDynamicDto, TEntity, TPrimaryKey>(dto, entity);
        }

        #endregion
    }
}

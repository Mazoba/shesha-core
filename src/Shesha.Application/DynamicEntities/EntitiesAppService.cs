using Abp.Authorization;
using Abp.Domain.Entities;
using Abp.ObjectMapping;
using Microsoft.AspNetCore.Mvc;
using Shesha.Application.Services;
using Shesha.Application.Services.Dto;
using Shesha.Configuration.Runtime;
using Shesha.Configuration.Runtime.Exceptions;
using Shesha.Utilities;
using System;
using System.Threading.Tasks;

namespace Shesha.DynamicEntities
{
    [AbpAuthorize()]
    public class EntitiesAppService: SheshaAppServiceBase
    {
        private readonly IEntityConfigurationStore _entityConfigStore;
        public IObjectMapper AutoMapper { get; set; }

        public EntitiesAppService(IEntityConfigurationStore entityConfigStore)
        {
            _entityConfigStore = entityConfigStore;
        }

        [HttpGet]
        public virtual async Task<IDynamicDataResult> GetAsync(string entityType, GetDynamicEntityInput<string> input) 
        {
            try
            {
                var entityConfig = _entityConfigStore.Get(entityType);
                if (entityConfig == null)
                    throw new EntityTypeNotFoundException(entityType);

                var appServiceType = entityConfig.ApplicationServiceType;

                if (entityConfig.ApplicationServiceType == null)
                    throw new NotSupportedException($"{nameof(entityConfig.ApplicationServiceType)} is not set for entity of type {entityConfig.EntityType.FullName}");

                var appService = IocManager.Resolve(appServiceType) as IEntityAppService;
                if (appService == null)
                    throw new NotImplementedException($"{nameof(IEntityAppService)} is not implemented by type {entityConfig.ApplicationServiceType.FullName}");

                // parse id value to concrete type
                var parsedId = Parser.ParseId(input.Id, entityConfig.EntityType);

                // make generic type
                //var invokerType = typeof(IEntityAppService<,>).MakeGenericType(entityConfig.EntityType, entityConfig.IdType);

                var methodName = nameof(IEntityAppService<Entity<Int64>, Int64>.QueryAsync);
                var method = appService.GetType().GetMethod(methodName);
                if (method == null)
                    throw new NotSupportedException($"{methodName} is missing in the {entityConfig.EntityType.FullName}");

                // invoke query
                var convertedInputType = typeof(GetDynamicEntityInput<>).MakeGenericType(entityConfig.IdType);
                var convertedInput = Activator.CreateInstance(convertedInputType);
                AutoMapper.Map(input, convertedInput);

                var task = (Task)method.Invoke(appService, new object[] { convertedInput });
                await task.ConfigureAwait(false);

                var resultProperty = task.GetType().GetProperty("Result");
                if (resultProperty == null)
                    throw new Exception("Result property not found");

                var data = resultProperty.GetValue(task) as IDynamicDataResult;
                if (data == null)
                    throw new Exception("Failed to fetch entity");

                return data;
            }
            catch (Exception e)
            {
                throw;
            }

        }

        [HttpGet]
        public virtual async Task<IDynamicDataResult> GetAllAsync(string entityType, PropsFilteredPagedAndSortedResultRequestDto input)
        {
            try
            {
                var entityConfig = _entityConfigStore.Get(entityType);
                if (entityConfig == null)
                    throw new EntityTypeNotFoundException(entityType);

                var appServiceType = entityConfig.ApplicationServiceType;

                if (entityConfig.ApplicationServiceType == null)
                    throw new NotSupportedException($"{nameof(GetAllAsync)} is not implemented for entity of type {entityConfig.EntityType.FullName}");

                var appService = IocManager.Resolve(appServiceType) as IEntityAppService;
                if (appService == null)
                    throw new NotImplementedException($"{nameof(IEntityAppService)} is not implemented by type {entityConfig.ApplicationServiceType.FullName}");

                return await appService.QueryAllAsync(input);
            }
            catch (Exception e) 
            {
                throw;                
            }
        }
    }
}

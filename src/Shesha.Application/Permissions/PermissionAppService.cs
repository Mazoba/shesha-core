using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Localization;
using Microsoft.AspNetCore.Authorization;
using Shesha.Domain;
using Shesha.Roles.Dto;

namespace Shesha.Permissions
{
    [AbpAuthorize()]
    public class PermissionAppService : SheshaAppServiceBase
    {
        //private readonly IPermissionDefinitionContext _permissionDefinitionContext;
        private readonly IRepository<PermissionDefinition, Guid> _permissionDefinitionRepository;

        public PermissionAppService(
            //IPermissionDefinitionContext permissionDefinitionContext,
            IRepository<PermissionDefinition, Guid> permissionDefinitionRepository
            )
        {
            //_permissionDefinitionContext = permissionDefinitionContext;
            _permissionDefinitionRepository = permissionDefinitionRepository;
        }

        public Task<ListResultDto<PermissionDto>> GetAllPermissions()
        {
            var permissions = PermissionManager.GetAllPermissions();

            return Task.FromResult(new ListResultDto<PermissionDto>(
                ObjectMapper.Map<List<PermissionDto>>(permissions).OrderBy(p => p.DisplayName).ToList()
            ));
        }

        public async Task<PermissionDto> CreatePermission(PermissionDto permission)
        {
            var dbp = new PermissionDefinition()
            {
                Name = permission.Name,
                DisplayName = permission.DisplayName,
                Description = permission.Description
            };
            _permissionDefinitionRepository.InsertOrUpdate(dbp);

            return ObjectMapper.Map<PermissionDto>(
                (PermissionManager as IPermissionDefinitionContext)?.CreatePermission(
                        permission.Name, 
                        L(permission.DisplayName),
                        L(permission.Description))
                );
        }

        private static ILocalizableString L(string name)
        {
            return new LocalizableString(name, SheshaConsts.LocalizationSourceName);
        }
    }
}
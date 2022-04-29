using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shesha.AutoMapper.Dto;
using Shesha.Domain;
using Shesha.Roles.Dto;

namespace Shesha.Permissions
{
    [AbpAuthorize()]
    public class PermissionAppService : SheshaAppServiceBase
    {
        //private readonly IPermissionDefinitionContext _permissionDefinitionContext;
        private readonly IRepository<PermissionDefinition, Guid> _permissionDefinitionRepository;
        private readonly ILocalizationContext _localizationContext;

        public PermissionAppService(
            //IPermissionDefinitionContext permissionDefinitionContext,
            IRepository<PermissionDefinition, Guid> permissionDefinitionRepository,
            ILocalizationContext localizationContext
            )
        {
            //_permissionDefinitionContext = permissionDefinitionContext;
            _permissionDefinitionRepository = permissionDefinitionRepository;
            _localizationContext = localizationContext;
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

            // ToDo: AS - Move to the Permission manager or extension
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

        [HttpGet]
        [AbpAuthorize()]
        public async Task<List<AutocompleteItemDto>> Autocomplete(string term)
        {
            term = (term ?? "").ToLower();
            
            var persons = PermissionManager.GetAllPermissions()
                .Where(p => (p.Name ?? "").ToLower().Contains(term)
                            || (p.Description.Localize(_localizationContext) ?? "").ToLower().Contains(term)
                            || (p.DisplayName.Localize(_localizationContext) ?? "").ToLower().Contains(term)
                            )
                .OrderBy(p => p.Name)
                .Take(10)
                .Select(p => new AutocompleteItemDto
                {
                    DisplayText = $"{p.DisplayName.Localize(_localizationContext)}{p.Name}",
                    Value = p.Name
                })
                .ToList();

            return persons;
        }

        private static ILocalizableString L(string name)
        {
            return new LocalizableString(name, SheshaConsts.LocalizationSourceName);
        }
    }
}
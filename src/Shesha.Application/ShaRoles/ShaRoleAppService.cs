using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Shesha.Authorization;
using Shesha.Domain;
using Shesha.Permissions.Dtos;
using Shesha.Roles.Dto;
using Shesha.ShaRoles.Dto;
using Shesha.Web.DataTable;

namespace Shesha.ShaRoles
{
    [AbpAuthorize(PermissionNames.Pages_Roles)]
    public class ShaRoleAppService : AsyncCrudAppService<ShaRole, ShaRoleDto, Guid, PagedRoleResultRequestDto, CreateShaRoleDto, ShaRoleDto>, IShaRoleAppService
    {
        private readonly IShaPermissionChecker _shaPermissionChecker;

        public ShaRoleAppService(
            IRepository<ShaRole, Guid> repository,
            IShaPermissionChecker shaPermissionChecker
            ) : base(repository)
        {
            _shaPermissionChecker = shaPermissionChecker;
        }

        /// <summary>
        /// Index table configuration 
        /// </summary>
        public static DataTableConfig IndexTable()
        {
            var table = new DataTableConfig<ShaRole, Guid>("ShaRoles_Index");

            table.AddProperty(e => e.Name, c => c.SortAscending());
            //table.AddProperty(e => e.NameSpace, c => c.HiddenByDefault());
            table.AddProperty(e => e.Description);

            table.AddProperty(e => e.CreationTime, c => c.Caption("Created On").Visible(false));
            table.AddProperty(e => e.LastModificationTime, c => c.Caption("Updated On").Visible(false));

            return table;
        }

        public override async Task<ShaRoleDto> CreateAsync(CreateShaRoleDto input)
        {
            CheckCreatePermission();

            var role = ObjectMapper.Map<ShaRole>(input);

            await Repository.InsertAsync(role);
            await CurrentUnitOfWork.SaveChangesAsync();

            return MapToEntityDto(role);
        }

        public override async Task<ShaRoleDto> UpdateAsync(ShaRoleDto input)
        {
            CheckUpdatePermission();

            var role = await Repository.GetAsync(input.Id);

            ObjectMapper.Map(input, role);

            await _shaPermissionChecker.ClearPermissionsCacheAsync();

            await Repository.UpdateAsync(role);
            await CurrentUnitOfWork.SaveChangesAsync();

            return MapToEntityDto(role);
        }

        public override async Task DeleteAsync(EntityDto<Guid> input)
        {
            CheckDeletePermission();

            var role = await Repository.GetAsync(input.Id);
            
            await Repository.DeleteAsync(role);
        }
    }
}


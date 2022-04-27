using Abp.Application.Services.Dto;
using Abp.AutoMapper;

namespace Shesha.Roles.Dto
{
    [AutoMapFrom(typeof(Abp.Authorization.Permission))]
    public class PermissionDto : EntityDto<long>
    {
        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string Description { get; set; }
    }
}

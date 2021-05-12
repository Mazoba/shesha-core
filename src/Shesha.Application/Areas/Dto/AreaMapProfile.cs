using System;
using Abp.Application.Services.Dto;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using AutoMapper;
using Shesha.AutoMapper;
using Shesha.AutoMapper.Dto;
using Shesha.Domain;
using Shesha.Services;

namespace Shesha.Areas.Dto
{
    public class AreaMapProfile : Profile
    {
        public AreaMapProfile()
        {
            CreateMap<AreaTreeItem, AreaTreeItemDto>();

            CreateMap<Area, AreaDto>()
                .ForMember(u => u.ParentArea, options => options.MapFrom(e => e.ParentArea != null ? new EntityWithDisplayNameDto<Guid?> { Id = e.ParentArea.Id, DisplayText = e.ParentArea.Name } : null))
                .MapReferenceListValuesToDto();

            CreateMap<AreaDto, Area>()
                .ForMember(u => u.ParentArea,
                    options => options.MapFrom(e =>
                        e.ParentArea != null && e.ParentArea.Id != null
                            ? GetEntity<Area, Guid>(e.ParentArea.Id.Value)
                            : null))
                .MapReferenceListValuesFromDto();

            CreateMap<AreaUpdateDto, Area>()
                .MapReferenceListValuesFromDto();

            CreateMap<AreaCreateDto, Area>()
                .ForMember(u => u.ParentArea,
                    options => options.MapFrom(e =>
                        e.ParentArea != null && e.ParentArea.Id != null
                            ? GetEntity<Area, Guid>(e.ParentArea.Id.Value)
                            : null))
                .MapReferenceListValuesFromDto();

        }

        // todo: implement automapper convention for nested entities
        private static T GetEntity<T, TId>(TId id) where T : class, IEntity<TId>
        {
            if (id == null || id is Guid guid && guid == Guid.Empty)
                return null;

            var repo = StaticContext.IocManager.Resolve<IRepository<T, TId>>();
            return repo.Get(id);
        }
    }
}

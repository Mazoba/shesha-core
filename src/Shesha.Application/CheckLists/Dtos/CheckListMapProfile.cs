using System;
using Shesha.AutoMapper;
using Shesha.CheckLists.Models;
using Shesha.Domain;

namespace Shesha.CheckLists.Dtos
{
    /// <summary>
    /// CheckList AutoMapper profile
    /// </summary>
    public class CheckListMapProfile : ShaProfile
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public CheckListMapProfile()
        {
            #region CheckList

            CreateMap<CheckListDto, CheckList>()
                .MapReferenceListValuesFromDto();

            CreateMap<CheckList, CheckListDto>()
                .MapReferenceListValuesToDto();

            #endregion

            #region Items

            CreateMap<CheckListItemDto, CheckListItem>()
                .ForMember(e => e.CheckList, m => m.MapFrom(e => GetEntity<CheckList>(e.CheckListId)))
                .ForMember(e => e.Parent, m => m.MapFrom(e => GetEntity<CheckListItem>(e.ParentId)))
                .MapReferenceListValuesFromDto();

            CreateMap<CheckListItem, CheckListItemDto>()
                .ForMember(e => e.CheckListId, m => m.MapFrom(e => e.CheckList != null ? e.CheckList.Id : Guid.Empty))
                .ForMember(e => e.ParentId, m => m.MapFrom(e => e.Parent != null ? e.Parent.Id : (Guid?)null))
                .MapReferenceListValuesToDto();

            #endregion

            CreateMap<CheckListTreeItem, CheckListTreeItemDto>();


            CreateMap<CheckListItemSelection, CheckListItemSelectionDto>()
                .ForMember(e => e.Name, opt => opt.MapFrom(e => e.CheckListItem.Name));


        }
    }
}

using AutoMapper;
using Shesha.AutoMapper;
using Shesha.AutoMapper.Dto;
using Shesha.Domain;
using Shesha.Domain.Enums;
using Shesha.Services;
using System;

namespace Shesha.Persons
{
    public class PersonMapProfile : ShaProfile
    {
        public PersonMapProfile()
        {
            CreateMap<CreatePersonAccountDto, Person>()
                .ForMember(u => u.EmailAddress1, options => options.MapFrom(e => e.EmailAddress))
                .ForMember(u => u.MobileNumber1, options => options.MapFrom(e => e.MobileNumber))
                .ForMember(u => u.PrimaryOrganisation, options => options.MapFrom(e => e.PrimaryOrganisation != null ? GetEntity<Organisation>(e.PrimaryOrganisation) : null))
                .ForMember(u => u.TypeOfAccount, options => options.MapFrom(e => e.TypeOfAccount != null ? (RefListTypeOfAccount?)e.TypeOfAccount.ItemValue : null));

            CreateMap<PersonAccountDto, Person>()
                .ForMember(u => u.EmailAddress1, options => options.MapFrom(e => e.EmailAddress))
                .ForMember(u => u.MobileNumber1, options => options.MapFrom(e => e.MobileNumber))
                .ForMember(u => u.PrimaryOrganisation, options => options.MapFrom(e => e.PrimaryOrganisation != null ? GetEntity<Organisation>(e.PrimaryOrganisation) : null))
                .ForMember(u => u.TypeOfAccount, options => options.MapFrom(e => e.TypeOfAccount != null ? (RefListTypeOfAccount?)e.TypeOfAccount.ItemValue : null));

            // TypeOfAccount

            CreateMap<Person, PersonAccountDto>()
                .ForMember(u => u.EmailAddress, options => options.MapFrom(e => e.EmailAddress1))
                .ForMember(u => u.MobileNumber, options => options.MapFrom(e => e.MobileNumber1))
                .ForMember(u => u.UserName, options => options.MapFrom(e => e.User != null ? e.User.UserName : null))
                .ForMember(u => u.PrimaryOrganisation, options => options.MapFrom(e => e.PrimaryOrganisation != null ? new EntityWithDisplayNameDto<Guid?>(e.PrimaryOrganisation.Id, e.PrimaryOrganisation.Name) : null))
                .ForMember(u => u.TypeOfAccount, options => options.MapFrom(e => GetRefListItemValueDto("Shesha.Core", "TypeOfAccount", (int?)e.TypeOfAccount)));

        }

        // todo: implement automapper convention for reference lists
        private static ReferenceListItemValueDto GetRefListItemValueDto(string refListNamespace, string refListName, int? value)
        {
            return value != null
                ? new ReferenceListItemValueDto
                {
                    ItemValue = value.Value,
                    Item = GetRefListItemText(refListNamespace, refListName, value)
                }
                : null;
        }

        // todo: implement automapper convention for reference lists
        private static string GetRefListItemText(string refListNamespace, string refListName, int? value)
        {
            if (value == null)
                return null;
            var helper = StaticContext.IocManager.Resolve<IReferenceListHelper>();
            return helper.GetItemDisplayText(refListNamespace, refListName, value);
        }
    }
}

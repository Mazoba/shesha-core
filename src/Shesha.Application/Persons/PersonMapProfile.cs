using Shesha.AutoMapper;
using Shesha.AutoMapper.Dto;
using Shesha.Domain;
using System;

namespace Shesha.Persons
{
    public class PersonMapProfile : ShaProfile
    {
        public PersonMapProfile()
        {
            CreateMap<CreatePersonAccountDto, Person>()
                .ForMember(u => u.EmailAddress1, options => options.MapFrom(e => e.EmailAddress))
                .ForMember(u => u.MobileNumber1, options => options.MapFrom(e => e.MobileNumber));

            CreateMap<PersonAccountDto, Person>()
                .ForMember(u => u.EmailAddress1, options => options.MapFrom(e => e.EmailAddress))
                .ForMember(u => u.MobileNumber1, options => options.MapFrom(e => e.MobileNumber));

            CreateMap<Person, PersonAccountDto>()
                .ForMember(u => u.EmailAddress, options => options.MapFrom(e => e.EmailAddress1))
                .ForMember(u => u.MobileNumber, options => options.MapFrom(e => e.MobileNumber1))
                .ForMember(u => u.UserName, options => options.MapFrom(e => e.User != null ? e.User.UserName : null));

        }
    }
}

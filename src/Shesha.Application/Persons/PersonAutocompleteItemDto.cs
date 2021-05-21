using System;
using Abp.Application.Services.Dto;

namespace Shesha.Persons
{
    public class PersonAutocompleteItemDto: EntityDto<Guid>
    {
        public string FullName { get; set; }
    }
}

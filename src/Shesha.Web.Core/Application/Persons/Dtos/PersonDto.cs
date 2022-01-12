using Abp.Application.Services.Dto;
using System;

namespace Shesha.Application.Persons.Dtos
{
    public class PersonDto: EntityDto<Guid>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}

using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Shesha.Domain;
using Shesha.DynamicEntities.Dtos;
using System;

namespace Shesha.Application.Persons
{
    [AbpAuthorize]
    public class PersonTestAppService : DynamicCrudAppService<Person, DynamicDto<Person, Guid>, Guid>, ITransientDependency
    {
        public PersonTestAppService(IRepository<Person, Guid> repository) : base(repository)
        {
        }
    }
}

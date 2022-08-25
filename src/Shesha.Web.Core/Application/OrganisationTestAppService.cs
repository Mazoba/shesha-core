using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Shesha.Domain;
using Shesha.DynamicEntities;
using Shesha.DynamicEntities.Dtos;
using Shesha.Specifications;
using System;
using System.Threading.Tasks;

namespace Shesha.Application
{
    [AbpAuthorize]
    public class OrganisationTestAppService : DynamicCrudAppService<Organisation, DynamicDto<Organisation, Guid>, Guid>, ITransientDependency
    {
        public OrganisationTestAppService(IRepository<Organisation, Guid> repository) : base(repository)
        {
        }

        [DisableSpecifications]
        public async Task GetUnfilteredAsync() 
        {
            var persons = await AsyncQueryableExecuter.ToListAsync(Repository.GetAll());
        }

        public async Task GetDefaultFilteredAsync()
        {
            var persons = await AsyncQueryableExecuter.ToListAsync(Repository.GetAll());
        }

        [ApplySpecifications(typeof(Age18PlusSpecification), typeof(HasNoAccountSpecification))]
        public async Task GetFilteredAsync()
        {
            var persons = await AsyncQueryableExecuter.ToListAsync(Repository.GetAll());
        }
    }
}
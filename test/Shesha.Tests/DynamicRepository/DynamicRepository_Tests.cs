using System.Threading.Tasks;
using Abp.Domain.Uow;
using Shesha.MultiTenancy;
using Shesha.Services;
using Shouldly;
using Xunit;

namespace Shesha.Tests.DynamicRepository
{
    public class DynamicRepository_Tests: SheshaNhTestBase
    {
        [Fact]
        public async Task<bool> GetEntity_Test()
        {
            var uow = Resolve<ICurrentUnitOfWorkProvider>();
            

            var rep = Resolve<IDynamicRepository>();
            var user = await rep.GetAsync(typeof(Tenant), "1");
            
            user.ShouldNotBeNull();

            return true;
        }
    }
}

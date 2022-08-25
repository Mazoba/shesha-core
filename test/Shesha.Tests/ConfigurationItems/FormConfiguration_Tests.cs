using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Shesha.Domain.ConfigurationItems;
using Shesha.Web.FormsDesigner.Domain;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Shesha.Tests.ConfigurationItems
{
    public class FormConfiguration_Tests: SheshaNhTestBase
    {
        [Fact]
        public async Task CreateFormConfig_Test()
        {
            var formConfigRepo = Resolve<IRepository<FormConfiguration, Guid>>();
            var configItemRepo = Resolve<IRepository<ConfigurationItem, Guid>>();
            
            var uowManager = Resolve<IUnitOfWorkManager>();

            try
            {
                using (var uow = uowManager.Begin())
                {
                    var form = new FormConfiguration();
                    form.Configuration.Name = "Test form";

                    form.Normalize();
                    await configItemRepo.InsertAsync(form.Configuration);
                    await formConfigRepo.InsertAsync(form);

                    var fetchedForm = await formConfigRepo.GetAsync(form.Id);

                    await uow.CompleteAsync();
                }
            }
            catch (Exception)
            {
                //throw;
            }

            try
            {
                using (var uow = uowManager.Begin())
                {
                    var form = new FormConfiguration();
                    form.Configuration.Name = "Test form 2";

                    //await configItemRepo.InsertAsync(form.Configuration);
                    form.Normalize();
                    await formConfigRepo.InsertAsync(form);

                    var fetchedForm = await formConfigRepo.GetAsync(form.Id);

                    await uow.CompleteAsync();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}

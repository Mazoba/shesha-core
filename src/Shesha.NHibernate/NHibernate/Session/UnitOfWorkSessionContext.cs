using Abp.Dependency;
using Abp.Domain.Uow;
using NHibernate;
using NHibernate.Context;
using Shesha.NHibernate.UoW;
using Shesha.Services;

namespace Shesha.NHibernate.Session
{
    public class UnitOfWorkSessionContext: ICurrentSessionContext
    {
        public UnitOfWorkSessionContext()
        {
            
        }

        public ISession CurrentSession()
        {
            var uowProvider = StaticContext.IocManager.Resolve<ICurrentUnitOfWorkProvider>();

            return uowProvider.Current is NhUnitOfWork nhUow
                ? nhUow.Session
                : null;
        }
    }
}

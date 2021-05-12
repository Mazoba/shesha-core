using Abp.EntityHistory;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using NHibernate;
using Shesha.NHibernate.EntityHistory;

namespace Shesha.NHibernate
{
    internal class SheshaNHibernateInstaller : IWindsorInstaller
    {
        public SheshaNHibernateInstaller()
        {
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<ISession>().UsingFactory<ISessionFactory, ISession>(f => f.OpenSession()).LifestyleScoped(),
                Component.For<NHibernateEntityHistoryStore>().ImplementedBy(typeof(NHibernateEntityHistoryStore)).LifestyleTransient(),
                Component.For<EntityHistoryHelperBase>().ImplementedBy(typeof(EntityHistory.EntityHistoryHelper)).LifestyleTransient()
            );
        }
    }
}

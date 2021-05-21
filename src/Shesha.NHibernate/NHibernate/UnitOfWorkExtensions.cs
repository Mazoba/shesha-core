using System;
using Abp.Domain.Uow;
using NHibernate;
using Shesha.NHibernate.UoW;

namespace Shesha.NHibernate
{
    public static class UnitOfWorkExtensions
    {
        public static void DoWithoutFlush(this IActiveUnitOfWork uow, Action action)
        {
            if (!(uow is NhUnitOfWork nhUow))
                return;

            var previousFlushMode = nhUow.Session.FlushMode;

            // We do NOT want this to flush pending changes as checking for a duplicate should 
            // only compare the object against data that's already in the database
            nhUow.Session.FlushMode = FlushMode.Manual;

            action.Invoke();

            nhUow.Session.FlushMode = previousFlushMode;
        }
    }
}

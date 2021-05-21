using System;
using Abp.Domain.Uow;
using NHibernate;
using Shesha.NHibernate.UoW;

namespace Shesha.NHibernate.Uow
{
    internal static class UnitOfWorkExtensions
    {
        public static ISession GetSession(this IActiveUnitOfWork unitOfWork)
        {
            if (unitOfWork == null)
            {
                throw new ArgumentNullException(nameof(unitOfWork));
            }

            if (!(unitOfWork is NhUnitOfWork))
            {
                throw new ArgumentException("unitOfWork is not type of " + typeof(NhUnitOfWork).FullName, "unitOfWork");
            }

            return (unitOfWork as NhUnitOfWork).Session;
        }
    }
}
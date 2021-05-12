using System.Linq;
using Abp;
using Abp.Domain.Uow;
using Abp.Extensions;
using Shesha.NHibernate.UoW;

namespace Shesha.NHibernate.Uow
{
    /// <summary>
    /// NHibernate filter executer
    /// </summary>
    public class NhUnitOfWorkFilterExecuter : IUnitOfWorkFilterExecuter
    {
        /// inheritedDoc
        public void ApplyDisableFilter(IUnitOfWork unitOfWork, string filterName)
        {
            var session = unitOfWork.As<NhUnitOfWork>().Session;
            if (session.GetEnabledFilter(filterName) != null)
            {
                session.DisableFilter(filterName);
            }
        }

        /// inheritedDoc
        public void ApplyEnableFilter(IUnitOfWork unitOfWork, string filterName)
        {
            var session = unitOfWork.As<NhUnitOfWork>().Session;
            if (session.GetEnabledFilter(filterName) == null)
            {
                session.EnableFilter(filterName);

                // Note: NH doesn't store parameter values for disabled filters, so we have to apply values again (simply read values from the configuration and fill NH filter parameters)
                var filterConfig = unitOfWork.Filters.FirstOrDefault(f => f.FilterName == filterName);
                if (filterConfig == null)
                    throw new AbpException("Unknown filter name: " + filterName + ". Be sure this filter is registered before.");

                foreach (var filterParameterConfig in filterConfig.FilterParameters)
                {
                    // set NH filter parameter value
                    ApplyFilterParameterValue(unitOfWork, filterName, filterParameterConfig.Key, filterParameterConfig.Value);
                }
            }
        }

        /// inheritedDoc
        public void ApplyFilterParameterValue(IUnitOfWork unitOfWork, string filterName, string parameterName, object value)
        {
            var session = unitOfWork.As<NhUnitOfWork>().Session;

            var filter = session?.GetEnabledFilter(filterName);

            filter?.SetParameter(parameterName, value);
        }
    }
}

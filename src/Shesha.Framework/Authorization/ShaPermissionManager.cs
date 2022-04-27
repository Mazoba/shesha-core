using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Authorization;
using Abp.Collections.Extensions;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Localization;
using NHibernate.Linq;
using Shesha.Domain;

namespace Shesha.Authorization
{
    public class ShaPermissionManager : PermissionManager
    {
        private readonly IIocManager _iocManager;
        private readonly IRepository<PermissionDefinition, Guid> _permissionDefinitionRepository;
        private readonly IAuthorizationConfiguration _authorizationConfiguration;

        public ShaPermissionManager(
            IIocManager iocManager,
            IAuthorizationConfiguration authorizationConfiguration, 
            IUnitOfWorkManager unitOfWorkManager,
            IMultiTenancyConfig multiTenancyConfig,
            IRepository<PermissionDefinition, Guid> permissionDefinitionRepository
            ) : 
            base(iocManager, authorizationConfiguration, unitOfWorkManager, multiTenancyConfig)
        {
            _iocManager = iocManager;
            _authorizationConfiguration = authorizationConfiguration;
            _permissionDefinitionRepository = permissionDefinitionRepository;
        }
    }
}
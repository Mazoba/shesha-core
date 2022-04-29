using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Abp;
using Abp.Application.Features;
using Abp.Application.Services;
using Abp.Authorization;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Localization;
using Abp.Runtime.Caching;
using ConcurrentCollections;
using Shesha.Authorization.Dtos;
using Shesha.Permission;
using Shesha.Permissions;

namespace Shesha.Authorization
{
    public class ApiAuthorizationHelper : AuthorizationHelper
    {

        private readonly IAuthorizationConfiguration _authConfiguration;
        private readonly IPermissionedObjectManager _permissionedObjectManager;


        public ApiAuthorizationHelper(
            IFeatureChecker featureChecker,
            IAuthorizationConfiguration authConfiguration,
            IPermissionedObjectManager permissionedObjectManager
            ) : base(featureChecker, authConfiguration)
        {
            _authConfiguration = authConfiguration;
            _permissionedObjectManager = permissionedObjectManager;
        }

        public override async Task AuthorizeAsync(MethodInfo methodInfo, Type type)
        {
            await base.AuthorizeAsync(methodInfo, type);
            await CheckApiPermissionsAsync(methodInfo, type);
        }

        public override void Authorize(MethodInfo methodInfo, Type type)
        {
            base.Authorize(methodInfo, type);
        }

        protected virtual async Task CheckApiPermissionsAsync(MethodInfo methodInfo, Type type)
        {
            if (!_authConfiguration.IsEnabled)
            {
                return;
            }

            var shaServiceType = typeof(SheshaAppServiceBase);
            if (type == null || !shaServiceType.IsAssignableFrom(type))
                return;

            /*if (!AbpSession.UserId.HasValue)
            {
                throw new AbpAuthorizationException(
                    LocalizationManager.GetString(AbpConsts.LocalizationSourceName, "CurrentUserDidNotLoginToTheApplication")
                );
            }*/

            var permission = await _permissionedObjectManager.GetAsync($"{type.FullName}@{methodInfo.Name}");

            if (permission?.Permissions == null
                || !permission.Permissions.Any() 
                || permission.Inherited) return;

            // ToDo: add RequireAll flag
            PermissionChecker.Authorize(false , permission.Permissions.ToArray());
        }
    }
}
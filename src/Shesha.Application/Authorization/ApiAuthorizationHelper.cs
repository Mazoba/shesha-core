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
using Microsoft.AspNetCore.Mvc;
using Shesha.Authorization.Dtos;
using Shesha.Domain.Enums;
using Shesha.Extensions;
using Shesha.Permission;
using Shesha.Permissions;
using Shesha.Utilities;

namespace Shesha.Authorization
{
    public class ApiAuthorizationHelper : AuthorizationHelper, IApiAuthorizationHelper
    {

        private readonly IAuthorizationConfiguration _authConfiguration;
        private readonly IPermissionedObjectManager _permissionedObjectManager;

        public ApiAuthorizationHelper(
            IFeatureChecker featureChecker,
            IAuthorizationConfiguration authConfiguration,
            IPermissionedObjectManager permissionedObjectManager,
            ILocalizationManager localizationManager
            ): base(featureChecker, authConfiguration)
        {
            _authConfiguration = authConfiguration;
            _permissionedObjectManager = permissionedObjectManager;
        }

        public virtual async Task AuthorizeAsync(MethodInfo methodInfo, Type type)
        {
            if (!_authConfiguration.IsEnabled)
            {
                return;
            }

            var shaServiceType = typeof(ApplicationService);
            var controllerType = typeof(ControllerBase);
            if (type == null || !shaServiceType.IsAssignableFrom(type) && !controllerType.IsAssignableFrom(type))
                return;

            /*if (!AbpSession.UserId.HasValue)
            {
                throw new AbpAuthorizationException(
                    LocalizationManager.GetString(AbpConsts.LocalizationSourceName, "CurrentUserDidNotLoginToTheApplication")
                );
            }*/

            var isDynamic = type.GetInterfaces().Any(x =>
                x.IsGenericType &&
                x.GetGenericTypeDefinition() == typeof(IDynamicCrudAppService<,,>));

            // ToDo: move to a provider/manager
            var typeName = type.FullName;
            if (isDynamic)
            {
                var entityType = type.FindBaseGenericType(typeof(AbpAsyncCrudAppService<,,,,,,,>))?.GetGenericArguments()[0];
                typeName = $"{entityType?.Namespace}.Dynamic{entityType?.Name}CrudAppService";
            }

            var permission = await _permissionedObjectManager.GetAsync($"{typeName}@{methodInfo.Name}");

            if (permission != null && (
                permission.ActualAccess == (int)RefListPermissionedAccess.Disable
                || permission.ActualAccess == (int)RefListPermissionedAccess.AnyAuthenticated && AbpSession.UserId == null
                || permission.ActualAccess == (int)RefListPermissionedAccess.RequiresPermissions
                && (permission.ActualPermissions == null || !permission.ActualPermissions.Any())
            ))
            {
                throw new AbpAuthorizationException(
                    LocalizationManager.GetString(SheshaConsts.LocalizationSourceName, "AccessDenied")
                );
            }

            if (permission == null
                || permission.ActualAccess == (int)RefListPermissionedAccess.AllowAnonymous
                || permission.ActualAccess == (int)RefListPermissionedAccess.AnyAuthenticated && AbpSession.UserId != null
                || permission.ActualPermissions == null 
                || !permission.ActualPermissions.Any())
                return;

            // ToDo: add RequireAll flag
            await PermissionChecker.AuthorizeAsync(false , permission.ActualPermissions.ToArray());
        }
    }
}
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Abp;
using Abp.Application.Features;
using Abp.Authorization;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Localization;
using Abp.Runtime.Caching;
using ConcurrentCollections;
using Shesha.Authorization.Dtos;

namespace Shesha.Authorization
{
    public class ApiAuthorizationHelper : AuthorizationHelper
    {

        private readonly IAuthorizationConfiguration _authConfiguration;
        private readonly ICacheManager _cacheManager;


        public ApiAuthorizationHelper(
            IFeatureChecker featureChecker,
            IAuthorizationConfiguration authConfiguration,
            ICacheManager cacheManager
            ) : base(featureChecker, authConfiguration)
        {
            _authConfiguration = authConfiguration;
            _cacheManager = cacheManager;
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

            if (type == null || !typeof(SheshaAppServiceBase).IsAssignableFrom(type))
                return;

            /*if (!AbpSession.UserId.HasValue)
            {
                throw new AbpAuthorizationException(
                    LocalizationManager.GetString(AbpConsts.LocalizationSourceName, "CurrentUserDidNotLoginToTheApplication")
                );
            }*/

            var cacheKey = type.FullName + "@" + methodInfo.Name;
            var customPermissionsItem = await _cacheManager.GetApiPermissionCache().GetOrDefaultAsync(cacheKey);

            if (customPermissionsItem == null)
            {
                // ToDo: get data from the storage (DB)
                // temporary
                customPermissionsItem = new RequiredPermissionCacheItem() 
                {
                    RequiredPermissions = new ConcurrentHashSet<string> {"test:permission"}
                };

                await _cacheManager.GetApiPermissionCache().SetAsync(cacheKey, customPermissionsItem, slidingExpireTime: TimeSpan.FromMinutes(5));
            }

            // ToDo: add RequireAll flag
            PermissionChecker.Authorize(false , customPermissionsItem.RequiredPermissions.ToArray());
        }
    }
}
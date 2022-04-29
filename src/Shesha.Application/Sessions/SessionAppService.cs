﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Auditing;
using Abp.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using NHibernate.Linq;
using Shesha.Authorization;
using Shesha.Domain;
using Shesha.Sessions.Dto;

namespace Shesha.Sessions
{
    public class SessionAppService : SheshaAppServiceBase, ISessionAppService
    {
        private readonly IRepository<ShaRoleAppointedPerson, Guid> _roleAppointmentRepository;
        private readonly IShaPermissionChecker _permissionChecker;

        public SessionAppService(IRepository<ShaRoleAppointedPerson, Guid> roleAppointmentRepository, IShaPermissionChecker permissionChecker)
        {
            _roleAppointmentRepository = roleAppointmentRepository;
            _permissionChecker = permissionChecker;
        }

        //[DisableAuditing]
        public async Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformations()
        {
            var output = new GetCurrentLoginInformationsOutput
            {
                Application = new ApplicationInfoDto
                {
                    Version = AppVersionHelper.Version,
                    ReleaseDate = AppVersionHelper.ReleaseDate,
                    Features = new Dictionary<string, bool>()
                }
            };

            if (AbpSession.TenantId.HasValue)
            {
                output.Tenant = ObjectMapper.Map<TenantLoginInfoDto>(await GetCurrentTenantAsync());
            }

            if (AbpSession.UserId.HasValue)
            {
                var user = await GetCurrentUserAsync();
                output.User = new UserLoginInfoDto
                {
                    AccountFound = true,
                    UserName = user.UserName,
                    FirstName = user.Name,
                    LastName = user.Surname,
                    FullName = user.FullName,
                    Email = user.EmailAddress,
                    MobileNumber = user.PhoneNumber,
                    GrantedPermissions = await GetGrantedPermissions()
                };
                //output.User = ObjectMapper.Map<UserLoginInfoDto>(await GetCurrentUserAsync());
            }

            return output;
        }

        private async Task<List<string>> GetGrantedPermissions()
        {
            var grantedPermissions = new List<string>();

            if (AbpSession.UserId.HasValue)
            {
                var allPermissionNames = PermissionManager.GetAllPermissions(false).Select(p => p.Name).ToList();
                
                foreach (var permissionName in allPermissionNames)
                {
                    if (await PermissionChecker.IsGrantedAsync(permissionName))
                        grantedPermissions.Add(permissionName);
                }
            }

            return grantedPermissions;
        }

      
        /// <summary>
        /// I am using this method to get user roles and it is being used on login of a user and also when changing work Order Type, Please contact me(Moses) before removing it
        /// </summary>
        /// <returns></returns>
        [DisableAuditing]
        public async Task<List<string>> GetGrantedShaRoles()
        {
            var currentUser = AbpSession.UserId.HasValue
                ? await GetCurrentPersonAsync()
                : null;
            if (currentUser == null)
                return new List<string>();
            var roles = await _roleAppointmentRepository.GetAll()
                .Where(a => a.Person == currentUser)
                .Select(a => a.Role.Name)
                .Distinct()
                .ToListAsync();
            return roles;
        }

        /// <summary>
        /// Clears permissions cache
        /// </summary>
        [HttpPost]
        public async Task ClearPermissionsCache()
        {
            await _permissionChecker.ClearPermissionsCacheAsync();
        }
    }
}

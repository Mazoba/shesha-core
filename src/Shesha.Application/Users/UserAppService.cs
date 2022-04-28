using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.IdentityFramework;
using Abp.Linq.Extensions;
using Abp.Localization;
using Abp.Runtime.Session;
using Abp.UI;
using Abp.Web.Models.AbpUserConfiguration;
using Shesha.Authorization;
using Shesha.Authorization.Accounts;
using Shesha.Roles.Dto;
using Shesha.Users.Dto;
using Microsoft.AspNetCore.Identity;
using NHibernate.Linq;
using Shesha.Authorization.Roles;
using Shesha.Authorization.Users;
using Shesha.Domain.Enums;
using Shesha.Domain;
using Shesha.NHibernate.EntityHistory;
using Shesha.Otp;
using Shesha.Otp.Dto;
using Shesha.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace Shesha.Users
{
    [AbpAuthorize(PermissionNames.Pages_Users)]

    public class UserAppService : AsyncCrudAppService<User, UserDto, long, PagedUserResultRequestDto, CreateUserDto, UserDto>, IUserAppService
    {
        private readonly UserManager _userManager;
        private readonly RoleManager _roleManager;
        private readonly IRepository<Role> _roleRepository;
        private readonly IRepository<Person, Guid> _personRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IAbpSession _abpSession;
        private readonly LogInManager _logInManager;
        
        private readonly IOtpAppService _otpService;
        private readonly IRepository<User, long> _userRepository;

        public UserAppService(
            IRepository<User, long> repository,
            UserManager userManager,
            RoleManager roleManager,
            IRepository<Role> roleRepository,
            IRepository<Person, Guid> personRepository,
        IPasswordHasher<User> passwordHasher,
            IAbpSession abpSession,
            LogInManager logInManager,
            IOtpAppService otpService,
            IRepository<User, long> userRepository)
            : base(repository)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _roleRepository = roleRepository;
            _personRepository = personRepository;
            _passwordHasher = passwordHasher;
            _abpSession = abpSession;
            _logInManager = logInManager;
            _otpService = otpService;
            _userRepository = userRepository;
        }

        public override async Task<UserDto> CreateAsync(CreateUserDto input)
        {
            CheckCreatePermission();

            var user = ObjectMapper.Map<User>(input);

            user.TenantId = AbpSession.TenantId;
            user.IsEmailConfirmed = true;

            await _userManager.InitializeOptionsAsync(AbpSession.TenantId);

            CheckErrors(await _userManager.CreateAsync(user, input.Password));

            if (input.RoleNames != null)
            {
                CheckErrors(await _userManager.SetRolesAsync(user, input.RoleNames));
            }

            CurrentUnitOfWork.SaveChanges();

            return MapToEntityDto(user);
        }

        public override async Task<UserDto> UpdateAsync(UserDto input)
        {
            CheckUpdatePermission();

            var user = await _userManager.GetUserByIdAsync(input.Id);

            MapToEntity(input, user);

            CheckErrors(await _userManager.UpdateAsync(user));

            if (input.RoleNames != null)
            {
                CheckErrors(await _userManager.SetRolesAsync(user, input.RoleNames));
            }

            return await GetAsync(input);
        }

        public override async Task DeleteAsync(EntityDto<long> input)
        {
            var user = await _userManager.GetUserByIdAsync(input.Id);
            await _userManager.DeleteAsync(user);
        }

        [HttpPost]
        public async Task<bool> InactivateUser(long userId)
        {
            CheckUpdatePermission();

            var user = await _userManager.GetUserByIdAsync(userId);

            if (!user.IsActive)
                throw new InvalidOperationException("Cannot inactivate user. User is already inactive.");

            user.IsActive = false;

            CheckErrors(await _userManager.UpdateAsync(user));

            return true;
        }

        [HttpPost]
        public async Task<bool> ActivateUser(long userId)
        {
            CheckUpdatePermission();

            var user = await _userManager.GetUserByIdAsync(userId);

            if (user.IsActive)
                throw new InvalidOperationException("Cannot activate user. User is already active.");

            user.IsActive = true;

            CheckErrors(await _userManager.UpdateAsync(user));

            return true;
        }

        public async Task<ListResultDto<RoleDto>> GetRoles()
        {
            var roles = await _roleRepository.GetAllListAsync();
            return new ListResultDto<RoleDto>(ObjectMapper.Map<List<RoleDto>>(roles));
        }

        public async Task ChangeLanguage(ChangeUserLanguageDto input)
        {
            await SettingManager.ChangeSettingForUserAsync(
                AbpSession.ToUserIdentifier(),
                LocalizationSettingNames.DefaultLanguage,
                input.LanguageName
            );
        }

        protected override User MapToEntity(CreateUserDto createInput)
        {
            var user = ObjectMapper.Map<User>(createInput);
            user.SetNormalizedNames();
            return user;
        }

        protected override void MapToEntity(UserDto input, User user)
        {
            ObjectMapper.Map(input, user);
            user.SetNormalizedNames();
        }

        protected override UserDto MapToEntityDto(User user)
        {
            try
            {
                var userRoles = user.Roles.Select(ur => ur.RoleId).ToList();
                var roles = _roleManager.Roles.Where(r => userRoles.Contains(r.Id)).Select(r => r.NormalizedName);
                var userDto = base.MapToEntityDto(user);
                userDto.RoleNames = roles.ToArray();
                return userDto;
            }
            catch
            {
                throw;
            }
        }

        protected override IQueryable<User> CreateFilteredQuery(PagedUserResultRequestDto input)
        {
            return Repository.GetAllIncluding(x => x.Roles)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x => x.UserName.Contains(input.Keyword) || x.Name.Contains(input.Keyword) || x.EmailAddress.Contains(input.Keyword))
                .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive);
        }

        protected override async Task<User> GetEntityByIdAsync(long id)
        {
            var user = await Repository.GetAllIncluding(x => x.Roles).FirstOrDefaultAsync(x => x.Id == id);

            if (user == null)
            {
                throw new EntityNotFoundException(typeof(User), id);
            }

            return user;
        }

        protected override IQueryable<User> ApplySorting(IQueryable<User> query, PagedUserResultRequestDto input)
        {
            return query.OrderBy(r => r.UserName);
        }

        protected virtual void CheckErrors(IdentityResult identityResult)
        {
            identityResult.CheckErrors(LocalizationManager);
        }

        #region Reset password using OTP

        private async Task<User> GetUniqueUserByMobileNoAsync(string mobileNo)
        {
            var users = await _userRepository.GetAll().Where(u => u.PhoneNumber == mobileNo).ToListAsync();

            if (users.Count > 1)
                throw new UserFriendlyException("Found more than one user with the provided Mobile No");

            if (!users.Any())
                throw new UserFriendlyException("User with the specified `Mobile No` not found");

            return users.FirstOrDefault();
        }

        /// <summary>
        /// Send One-time pin for password reset
        /// </summary>
        /// <param name="mobileNo">mobile number of the user</param>
        [AbpAllowAnonymous]
        public async Task<ResetPasswordSendOtpResponse> ResetPasswordSendOtp(string mobileNo)
        {
            // todo: cleanup mobile number
            // todo: store clear mobile number in the DB

            // ensure that the user exists
            var user = await GetUniqueUserByMobileNoAsync(mobileNo);

            var otpResponse = await _otpService.SendPinAsync(new SendPinInput() { SendTo = mobileNo, SendType = OtpSendType.Sms });

            return new ResetPasswordSendOtpResponse()
            {
                OperationId = otpResponse.OperationId
            };
        }

        /// <summary>
        /// Verify one-time pin that was used for password reset. Returns a token that should be used for password update
        /// </summary>
        [AbpAllowAnonymous]
        public async Task<ResetPasswordVerifyOtpResponse> ResetPasswordVerifyOtp(ResetPasswordVerifyOtpInput input)
        {
            var otp = await _otpService.GetAsync(input.OperationId);
            var personId = otp?.RecipientId.ToGuid() ?? Guid.Empty;
            var user = personId != Guid.Empty
                ? (await _personRepository.GetAsync(personId))?.User
                : await GetUniqueUserByMobileNoAsync(input.MobileNo);

            if (user == null)
                throw new Exception("User not found");

            var otpRequest = ObjectMapper.Map<VerifyPinInput>(input);
            var otpResponse = await _otpService.VerifyPinAsync(otpRequest);

            var response = ObjectMapper.Map<ResetPasswordVerifyOtpResponse>(otpResponse);

            if (response.IsSuccess)
            {
                user.SetNewPasswordResetCode();
                await _userManager.UpdateAsync(user);
                
                // real password reset will be done using token
                response.Token = user.PasswordResetCode;
                response.Username = user.UserName;
            }

            return response;
        }

        /// <summary>
        /// Resets a password of the user using token
        /// </summary>
        [AbpAllowAnonymous]
        public async Task<bool> ResetPasswordUsingToken(ResetPasswordUsingTokenInput input)
        {
            var user = await _userRepository.GetAll().FirstOrDefaultAsync(u => u.UserName == input.Username);
            if (user == null)
                throw new UserFriendlyException("User not found");

            // check the token
            if (user.PasswordResetCode != input.Token)
                throw new UserFriendlyException("Your token is invalid or has expired, try to reset password again");

            // todo: add new setting for the PasswordRegex and error message
            if (!new Regex(AccountAppService.PasswordRegex).IsMatch(input.NewPassword))
            {
                throw new UserFriendlyException("Passwords must be at least 8 characters, contain a lowercase, uppercase, and number.");
            }
            
            user.AddHistoryEvent("Password reset", "Password reset");
            _personRepository.GetAll().FirstOrDefault(x => x.User == user)?.AddHistoryEvent("Password reset", "Password reset");

            user.Password = _passwordHasher.HashPassword(user, input.NewPassword);
            user.PasswordResetCode = null;

            CurrentUnitOfWork.SaveChanges();

            return true;
        }

        #endregion

        public async Task<bool> ChangePassword(ChangePasswordDto input)
        {
            if (_abpSession.UserId == null)
            {
                throw new UserFriendlyException("Please log in before attemping to change password.");
            }
            long userId = _abpSession.UserId.Value;
            var user = await _userManager.GetUserByIdAsync(userId);
            var loginAsync = await _logInManager.LoginAsync(user.UserName, input.CurrentPassword, shouldLockout: false);
            if (loginAsync.Result != ShaLoginResultType.Success)
            {
                throw new UserFriendlyException("Your 'Existing Password' did not match the one on record.  Please try again or contact an administrator for assistance in resetting your password.");
            }
            // todo: add new setting for the PasswordRegex and error message
            if (!new Regex(AccountAppService.PasswordRegex).IsMatch(input.NewPassword))
            {
                throw new UserFriendlyException("Passwords must be at least 8 characters, contain a lowercase, uppercase, and number.");
            }

            user.AddHistoryEvent("Password changed", "Password changed");
            _personRepository.GetAll().FirstOrDefault(x => x.User == user)?.AddHistoryEvent("Password changed", "Password changed");

            user.Password = _passwordHasher.HashPassword(user, input.NewPassword);
            CurrentUnitOfWork.SaveChanges();
            return true;
        }

        public async Task<bool> ResetPassword(ResetPasswordDto input)
        {
            if (_abpSession.UserId == null)
            {
                throw new UserFriendlyException("Please log in before attemping to reset password.");
            }
            long currentUserId = _abpSession.UserId.Value;
            var currentUser = await _userManager.GetUserByIdAsync(currentUserId);
            var loginAsync = await _logInManager.LoginAsync(currentUser.UserName, input.AdminPassword, shouldLockout: false);
            if (loginAsync.Result != ShaLoginResultType.Success)
            {
                throw new UserFriendlyException("Your 'Admin Password' did not match the one on record.  Please try again.");
            }
            if (currentUser.IsDeleted || !currentUser.IsActive)
            {
                return false;
            }
            
            if (!await PermissionChecker.IsGrantedAsync(ShaPermissionNames.Users_ResetPassword))
            {
                throw new UserFriendlyException("You are not authorized to reset passwords.");
            }

            var user = await _userManager.GetUserByIdAsync(input.UserId);
            if (user != null)
            {
                user.AddHistoryEvent("Password reset", "Password reset");
                _personRepository.GetAll().FirstOrDefault(x => x.User == user)?.AddHistoryEvent("Password reset", "Password reset");

                user.Password = _passwordHasher.HashPassword(user, input.NewPassword);
                CurrentUnitOfWork.SaveChanges();
            }

            return true;
        }

        public virtual async Task<AbpUserAuthConfigDto> GetUserAuthConfig()
        {
            var config = new AbpUserAuthConfigDto();

            var allPermissionNames = PermissionManager.GetAllPermissions(false).Select(p => p.Name).ToList();
            var grantedPermissionNames = new List<string>();

            if (AbpSession.UserId.HasValue)
            {
                foreach (var permissionName in allPermissionNames)
                {
                    if (await PermissionChecker.IsGrantedAsync(permissionName))
                    {
                        grantedPermissionNames.Add(permissionName);
                    }
                }
            }

            config.AllPermissions = allPermissionNames.ToDictionary(permissionName => permissionName, permissionName => "true");
            config.GrantedPermissions = grantedPermissionNames.ToDictionary(permissionName => permissionName, permissionName => "true");

            return config;
        }
    }
}


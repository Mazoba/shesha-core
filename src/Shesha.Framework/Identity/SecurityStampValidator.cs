using Abp.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shesha.Authorization.Roles;
using Shesha.Authorization.Users;
using Shesha.MultiTenancy;

namespace Shesha.Identity
{
    public class SecurityStampValidator : AbpSecurityStampValidator<Tenant, Role, User>
    {
        public SecurityStampValidator(
            IOptions<SecurityStampValidatorOptions> options, 
            SignInManager signInManager,
            ISystemClock systemClock,
            ILoggerFactory loggerFactory
            ) 
            : base(
                  options, 
                  signInManager, 
                  systemClock,
                  loggerFactory)
        {
        }
    }
}

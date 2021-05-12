using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Shesha.Roles.Dto;
using Shesha.Users.Dto;

namespace Shesha.Users
{
    public interface IUserAppService : IAsyncCrudAppService<UserDto, long, PagedUserResultRequestDto, CreateUserDto, UserDto>
    {
        Task<ListResultDto<RoleDto>> GetRoles();

        Task ChangeLanguage(ChangeUserLanguageDto input);

        Task<ResetPasswordSendOtpResponse> ResetPasswordSendOtp(string mobileNo);
        Task<bool> ResetPasswordUsingToken(ResetPasswordUsingTokenInput input);
        Task<ResetPasswordVerifyOtpResponse> ResetPasswordVerifyOtp(ResetPasswordVerifyOtpInput input);
    }
}

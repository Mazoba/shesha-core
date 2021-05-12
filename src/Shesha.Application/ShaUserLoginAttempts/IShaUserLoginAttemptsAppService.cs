using System;
using Abp.Application.Services;
using Shesha.ShaUserLoginAttempts.Dto;

namespace Shesha.ShaUserLoginAttempts
{
    public interface IShaUserLoginAttemptsAppService : IAsyncCrudAppService<ShaUserLoginAttemptDto, Guid>
    {
    }
}

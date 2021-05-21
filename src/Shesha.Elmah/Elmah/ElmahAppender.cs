using System;
using Abp.Dependency;
using ElmahCore;
using log4net.Appender;
using log4net.Core;
using Microsoft.AspNetCore.Http;
using Shesha.Services;

namespace Shesha.Elmah
{
    /// <summary>
    /// Elmah log4net Appender
    /// </summary>
    public class ElmahAppender: AppenderSkeleton
    {
        protected override void Append(LoggingEvent loggingEvent)
        {
            var exception = loggingEvent.ExceptionObject;

            if (exception == null || loggingEvent.ExceptionObject.IsExceptionLogged())
                return;

            var httpContextAccessor = StaticContext.IocManager.IsRegistered<IHttpContextAccessor>()
                ? StaticContext.IocManager.Resolve<IHttpContextAccessor>()
                : null;

            if (httpContextAccessor?.HttpContext != null)
                httpContextAccessor.HttpContext.RiseError(exception);
            else
                ElmahExtensions.RiseError(exception);

            exception.MarkExceptionAsLogged();
        }
	}
}

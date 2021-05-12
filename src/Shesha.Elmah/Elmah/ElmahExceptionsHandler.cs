using Abp.Dependency;
using Abp.Events.Bus.Exceptions;
using Abp.Events.Bus.Handlers;
using ElmahCore;

namespace Shesha.Elmah
{
    /// <summary>
    /// Handles all exceptions handled by Abp and logs them using Elmah
    /// </summary>
    public class ElmahExceptionsHandler : IEventHandler<AbpHandledExceptionData>, ITransientDependency
    {
        /// inheritDoc
        public void HandleEvent(AbpHandledExceptionData eventData)
        {
            if (eventData.Exception != null && !eventData.Exception.IsExceptionLogged())
            {
                ElmahExtensions.RiseError(eventData.Exception);
                eventData.Exception.MarkExceptionAsLogged();
            }
        }
    }
}

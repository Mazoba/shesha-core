using Abp.Dependency;
using Abp.Events.Bus.Exceptions;
using Abp.Events.Bus.Handlers;

namespace Shesha.Exceptions
{
    /// <summary>
    /// Exception event handler
    /// </summary>
    public class ExceptionEventHandler : IEventHandler<AbpHandledExceptionData>, ITransientDependency
    {
        public void HandleEvent(AbpHandledExceptionData eventData)
        {
            // log exception
            eventData.Exception.LogError();
        }
    }
}

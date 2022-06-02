using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Shesha.Authorization
{
    public interface IApiAuthorizationHelper
    {
        Task AuthorizeAsync(MethodInfo methodInfo, Type type);
    }
}
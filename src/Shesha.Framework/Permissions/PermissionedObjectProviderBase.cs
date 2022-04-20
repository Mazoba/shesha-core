using System;
using System.ComponentModel;
using System.Reflection;
using System.Xml;
using Abp.Dependency;
using Abp.Reflection;
using Shesha.Reflection;
using Shesha.Utilities;

namespace Shesha.Permissions
{
    public class PermissionedObjectProviderBase: ITransientDependency
    {
        protected readonly IAssemblyFinder _assembleFinder;

        public PermissionedObjectProviderBase(IAssemblyFinder assembleFinder)
        {
            _assembleFinder = assembleFinder;
        }

        protected string GetDescription(Type service)
        {
            var description = service.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
            if (string.IsNullOrEmpty(description))
            {
                description = service.GetCustomAttribute<DescriptionAttribute>()?.Description;
                if (string.IsNullOrEmpty(description))
                {
                    XmlElement documentation = DocsByReflection.XMLFromType(service);
                    description = documentation?["summary"]?.InnerText.Trim();
                    if (string.IsNullOrEmpty(description))
                    {
                        description = service.Name.ToFriendlyName();
                    }
                }
            }

            return description;
        }

        protected string GetDescription(MethodInfo method)
        {
            var description = method.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
            if (string.IsNullOrEmpty(description))
            {
                description = method.GetCustomAttribute<DescriptionAttribute>()?.Description;
                if (string.IsNullOrEmpty(description))
                {
                    XmlElement documentation = DocsByReflection.XMLFromMember(method);
                    description = documentation?["summary"]?.InnerText.Trim();
                    if (string.IsNullOrEmpty(description))
                    {
                        description = method.Name.ToFriendlyName().Replace(" Async", "");
                    }
                }
            }

            return description;
        }
    }
}
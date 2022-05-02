using Abp.Dependency;
using Abp.Runtime.Caching;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Shesha.Metadata.Dtos;
using Shesha.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.Metadata
{
    public class ApiModelProvider: BaseModelProvider, ITransientDependency
    {
        private readonly IApiDescriptionGroupCollectionProvider _apiDescriptionsProvider;

        public ApiModelProvider(ICacheManager cacheManager, IApiDescriptionGroupCollectionProvider apiDescriptionsProvider) : base (cacheManager)
        {
            _apiDescriptionsProvider = apiDescriptionsProvider;
        }

        protected override Task<List<ModelDto>> FetchModelsAsync()
        {
            var actionDescriptors = _apiDescriptionsProvider.ApiDescriptionGroups.Items.SelectMany(g => g.Items.Select(gi => gi.ActionDescriptor)).ToList();
            var parameters = actionDescriptors
                .Where(a => a.ActionConstraints.Any(ac => ac is HttpMethodActionConstraint httpConstraint &&
                    httpConstraint.HttpMethods.Any(m => m == System.Net.Http.HttpMethod.Post.ToString() || m == System.Net.Http.HttpMethod.Put.ToString())
                    )
                )
                .SelectMany(a => a.Parameters)
                .ToList();

            var parameterTypes = parameters
                .Select(p => p.ParameterType)
                .Distinct()
                .Where(t => t.IsClass &&
                    !t.IsGenericType &&
                    !t.IsAbstract &&
                    !t.IsArray &&
                    t != typeof(string) &&
                    t != typeof(object) &&
                    !t.Namespace.StartsWith("Abp"))
                .OrderBy(t => t.Name)
                .ToList();

            var dtos = parameterTypes.Select(p => new ModelDto
                {
                    ClassName = p.FullName,
                    Type = p,
                    Description = ReflectionHelper.GetDescription(p),
                    Alias = null
                })
                .ToList();

            return Task.FromResult(dtos);
        }
    }
}

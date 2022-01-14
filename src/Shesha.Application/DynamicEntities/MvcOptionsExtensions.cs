using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Logging.Abstractions;
using Shesha.DynamicEntities.Dtos;
using Shesha.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.DynamicEntities
{
    /// <summary>
    /// MvcOptions extensions
    /// </summary>
    public static class MvcOptionsExtensions
    {
        /// <summary>
        /// Enable binding of the <see cref="DynamicDto{TEntity, TId}"/>
        /// </summary>
        /// <param name="options"></param>
        public static void EnableDynamicDtoBinding(this MvcOptions options) 
        {
            var bodyBuilder = options.ModelBinderProviders.FirstOrDefault(b => b.GetType() == typeof(BodyModelBinderProvider));
            var idx = bodyBuilder != null
                ? options.ModelBinderProviders.IndexOf(bodyBuilder)
                : options.ModelBinderProviders.Count - 1;

            var readerFactory = StaticContext.IocManager.Resolve<IHttpRequestStreamReaderFactory>();
            var dynamicDtoBinderProvider = new DynamicDtoBinderProvider(options.InputFormatters, readerFactory, NullLoggerFactory.Instance, options);

            options.ModelBinderProviders.Insert(idx, dynamicDtoBinderProvider);
        }
    }
}

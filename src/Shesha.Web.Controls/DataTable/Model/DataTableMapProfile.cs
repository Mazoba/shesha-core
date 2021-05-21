using System;
using Abp.Dependency;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Shesha.AutoMapper;
using Shesha.Services;

namespace Shesha.Web.DataTable.Model
{
    public class DataTableMapProfile: ShaProfile
    {
        public DataTableMapProfile()
        {
            CreateMap<DataTableConfig, DataTableConfigDto>()
                .ForMember(c => c.CreateUrl, m => m.MapFrom(c => GetUrl(c.CreateUrl)))
                .ForMember(c => c.DetailsUrl, m => m.MapFrom(c => GetUrl(c.DetailsUrl)))
                .ForMember(c => c.UpdateUrl, m => m.MapFrom(c => GetUrl(c.UpdateUrl)))
                .ForMember(c => c.DeleteUrl, m => m.MapFrom(c => GetUrl(c.DeleteUrl)))
                ;
        }

        private static string GetUrl(Func<IUrlHelper, string> urlFunc)
        {
            if (urlFunc == null)
                return null;

            var actionContextAccessor = StaticContext.IocManager.Resolve<IActionContextAccessor>();
            var urlHelperFactory = StaticContext.IocManager.Resolve<IUrlHelperFactory>();
            
            var url = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);

            return urlFunc.Invoke(url);
        }
    }
}

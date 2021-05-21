using System.Collections.Generic;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Shesha.Web.FormsDesigner.Legacy
{
    public interface IPropertyGridDataProvider
    {
        object Model { get; }
        List<CategoryInfoModel> GetGroups(PropertyGridConfig config);
        IHtmlContent RenderDisplay(HtmlHelper htmlHelper, ModelPropertyConfig property);
        IHtmlContent RenderEdit(HtmlHelper htmlHelper, ModelPropertyConfig property);
    }
}

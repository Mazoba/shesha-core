using System;
using System.Collections.Generic;
using System.Text;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Shesha.Services;
using Shesha.Web.FormsDesigner.Domain;

namespace Shesha.Web.FormsDesigner.Legacy
{
    [FormComponentProps(SubFormComponent.Type)]
    public class SubFormComponent : ComponentBase<SubFormModel>
    {
        public const string Type = "subform";

        protected override object DoGetContextData<TModel>(SubFormModel componentModel, TModel model)
        {
            var formService = StaticContext.IocManager.Resolve<IRepository<Form, Guid>>();

            var form = componentModel.FormId.HasValue
                ? formService.Get(componentModel.FormId.Value)
                : null;

            //var urlHelper = SheshaContext.Current.Controller.Url;

            return new
            {
                FormName = form?.Name,
                FormDescription = form?.Description,
                /*
                FormUrl = form != null
                    ? urlHelper.SPAAction("Designer", "Form", new { id = form.Id })
                    : null
                    */
            };
        }
    }

    public class SubFormModel : ComponentModelBase
    {
        public Guid? FormId { get; set; }
    }
}

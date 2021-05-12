using System;
using System.Collections.Generic;
using System.Text;

namespace Shesha.Web.FormsDesigner.Legacy
{
    public interface IFormComponentOld
    {
        void BindChildComponents(FormComponent component, ComponentModelBase model);

        object GetModel<TModel>(ComponentModelBase componentModel, TModel model);

        void BindModel<TModel>(ComponentModelBase componentModel, TModel model, out List<string> errorMessages);

        object GetContextData<TModel>(ComponentModelBase componentModel, TModel model);

        List<string> GetModelKeys(ComponentModelBase model);

        bool ShouldBeBound(ComponentModelBase componentModel);
    }
}

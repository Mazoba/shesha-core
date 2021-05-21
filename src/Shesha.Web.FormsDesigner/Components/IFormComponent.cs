using System.Collections.Generic;
using Shesha.Web.FormsDesigner.Legacy;

namespace Shesha.Web.FormsDesigner.Components
{
    /// <summary>
    /// Interface of the form component
    /// </summary>
    public interface IFormComponent
    {
        /// <summary>
        /// Bind child components
        /// </summary>
        /// <param name="component"></param>
        /// <param name="model"></param>
        void BindChildComponents(FormComponent component, ComponentModelBase model);

        /// <summary>
        /// Get model of the component
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="componentModel"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        object GetModel<TModel>(ComponentModelBase componentModel, TModel model);

        /// <summary>
        /// Bind component value to the model
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="componentModel"></param>
        /// <param name="model"></param>
        /// <param name="errorMessages"></param>
        void BindModel<TModel>(ComponentModelBase componentModel, TModel model, out List<string> errorMessages);

        /// <summary>
        /// Get context data of the component
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="componentModel"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        object GetContextData<TModel>(ComponentModelBase componentModel, TModel model);

        /// <summary>
        /// Get model keys
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        List<string> GetModelKeys(ComponentModelBase model);

        /// <summary>
        /// Returns tru if the component should be bound to the model
        /// </summary>
        /// <param name="componentModel"></param>
        /// <returns></returns>
        bool ShouldBeBound(ComponentModelBase componentModel);
    }
}

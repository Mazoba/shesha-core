using System;
using System.Collections.Generic;
using System.Text;
using Abp.Domain.Entities;
using Shesha.Domain;
using Shesha.Extensions;
using Shesha.Reflection;
using Shesha.Web.FormsDesigner.Components;

namespace Shesha.Web.FormsDesigner.Legacy
{
    public abstract class ComponentBase<T> : IFormComponentOld where T : ComponentModelBase
    {
        public void BindChildComponents(FormComponent component, ComponentModelBase model)
        {
            DoBindChildComponents(component, model as T);
        }

        public virtual void DoBindChildComponents(FormComponent component, T model)
        {
        }

        public object GetModel<TModel>(ComponentModelBase componentModel, TModel model)
        {
            return DoGetModel(componentModel as T, model);
        }

        protected virtual object DoGetModel<TModel>(T componentModel, TModel model)
        {
            var entity = model as IEntity;

            if (entity != null && entity.IsTransient() && !string.IsNullOrWhiteSpace(componentModel.DefaultValue))
            {
                return componentModel.DefaultValue;
            }

            var propInfo = ReflectionHelper.GetProperty(model, componentModel.ApiKey, out var propertyEntity);
            if (propInfo != null)
            {
                return propertyEntity != null
                    ? propInfo.GetValue(propertyEntity, null)
                    : null;
                //return ReflectionHelper.GetPropertyValue(model, componentModel.ApiKey, null);
            }
            else if (componentModel.Persist)
            {
                /*
                // save to versioned fields if possible
                if (entity != null)
                {
                    var value = entity.GetVersionedFieldLastVersionContent(componentModel.ApiKey);
                    return ConvertVersionedFieldValueToModel(componentModel, value);
                }
                */
            }

            return null;
        }

        protected virtual object ConvertVersionedFieldValueToModel(T componentModel, string value)
        {
            return value;
        }

        public void BindModel<TModel>(ComponentModelBase componentModel, TModel model, out List<string> errorMessages)
        {
            DoBindModel(componentModel as T, model, out errorMessages);
        }

        public object GetContextData<TModel>(ComponentModelBase componentModel, TModel model)
        {
            return DoGetContextData(componentModel as T, model);
        }

        public List<string> GetModelKeys(ComponentModelBase model)
        {
            return new List<string> { model.ApiKey };
        }

        protected virtual object DoGetContextData<TModel>(T componentModel, TModel model)
        {
            return null;
        }

        protected virtual void DoBindModel<TModel>(T componentModel, TModel model, out List<string> errorMessages)
        {
            errorMessages = new List<string>();

            var value = GetFormValue(componentModel);
            ValidateValue(componentModel, model, value, errorMessages);

            var propInfo = ReflectionHelper.GetProperty(model, componentModel.ApiKey, out var propertyEntity);
            if (propInfo != null)
            {
                model.SetPropertyValue(componentModel.ApiKey, value);
            }
            else if (componentModel.Persist)
            {
                /*
                // save to versioned fields if possible
                if (model is IEntity entity)
                {
                    entity.SetVersionedFieldValue(componentModel.ApiKey, value, Authoriser.UserName, componentModel.TrackVersions, null, out var modified, DateTime.Now, componentModel.TrackVersions, fieldType: GetFieldType(componentModel));
                }
                */
            }
        }

        protected virtual string GetFieldType(T componentModel)
        {
            return null;
        }

        protected IEntity GetOwner<TModel>(string owner, TModel model)
        {
            if (string.IsNullOrWhiteSpace(owner))
                return model as IEntity;

            var type = model != null
                ? model.GetType()
                : typeof(TModel);
            var property = ReflectionHelper.GetProperty(type, owner);
            if (property == null)
                return null;

            return property.GetValue(model) as IEntity;
        }

        protected virtual string GetFormValue(T componentModel)
        {
            throw new NotImplementedException();
            //return ValueProvider.GetAttemptedValue(componentModel.ApiKey);
        }

        protected virtual void ValidateValue<TModel>(T componentModel, TModel model, object value, List<string> errorMessages)
        {
            if (value is string && componentModel.Required && string.IsNullOrWhiteSpace(value as string))
                errorMessages.Add("This field is required");
        }

        public bool ShouldBeBound(ComponentModelBase componentModel)
        {
            return GetShouldBeBound(componentModel as T);
        }

        protected virtual bool GetShouldBeBound(T componentModel)
        {
            return !componentModel.Disabled;
        }

        //protected IValueProvider ValueProvider => SheshaContext.Current?.Controller?.ValueProvider;
    }

}

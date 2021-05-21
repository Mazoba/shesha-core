using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using AutoMapper;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Shesha.AutoMapper;
using Shesha.Configuration.Runtime;
using Shesha.Extensions;
using Shesha.Reflection;
using Shesha.Services;
using Shesha.Utilities;
using Shesha.Web.FormsDesigner.Components;
using Shesha.Web.FormsDesigner.Domain;

namespace Shesha.Web.FormsDesigner.Legacy
{
    public static class FormDesignerHelper
    {
        private static readonly IRepository<Form, Guid> _formService;
        private static readonly IRepository<FormComponent, Guid> ComponentService;

        public const string VisibleComponentsHeader = "sha-visible-components";

        static FormDesignerHelper()
        {
            _formService = StaticContext.IocManager.Resolve<IRepository<Form, Guid>>();
            ComponentService = StaticContext.IocManager.Resolve<IRepository<FormComponent, Guid>>();
        }

        public static Form GetForm(Guid id)
        {
            return _formService.Get(id);
        }

        public static Type GetComponentModelType(string componentType)
        {
            return Components.ContainsKey(componentType)
                ? Components[componentType].ModelType
                : typeof(ComponentModelBase);
        }

        public static ComponentInfo GetComponentInfo(string componentType)
        {
            return Components.ContainsKey(componentType)
                ? Components[componentType]
                : new ComponentInfo()
                {
                    ModelType = typeof(ComponentModelBase),
                    Type = componentType
                };
        }

        public static IFormComponentOld GetComponentInstance(string componentType)
        {
            var componentInfo = GetComponentInfo(componentType);
            return Activator.CreateInstance(componentInfo.ComponentType) as IFormComponentOld;
        }

        private static Dictionary<string, ComponentInfo> _components;

        public static Dictionary<string, ComponentInfo> Components
        {
            get
            {
                if (_components != null)
                    return _components;



                var components = ReflectionHelper
                    .FilterTypesInAssemblies(t => t.IsSubtypeOfGeneric(typeof(ComponentBase<>)) && t.HasAttribute<FormComponentPropsAttribute>())
                    .Select(e => {
                        var attribute = e.GetAttribute<FormComponentPropsAttribute>();

                        var modelType = e.BaseType.GenericTypeArguments[0];

                        return new ComponentInfo()
                        {
                            Type = attribute.ComponentType,
                            BindChildProps = attribute.BindChilds,
                            IsInput = attribute.IsInput,
                            IsOutput = attribute.IsOutput,
                            JavascriptLibrary = attribute.JavascriptLibrary,
                            ComponentType = e,
                            ModelType = modelType,
                            CustomProperties = GetCustomProperties(modelType)
                        };
                    })
                    .ToList();

                var duplicates = components.GroupBy(i => i.Type, i => i, (type, items) => new { type, items })
                    .Where(g => g.items.Count() > 1).ToList();
                if (duplicates.Any())
                {
                    throw new Exception(
                        $"Components with duplicated {nameof(FormComponentPropsAttribute.ComponentType)} found: " +
                        duplicates.Select(i => i.type).Delimited("; "));
                }

                _components = components.ToDictionary(i => i.Type, i => i);

                return _components;
            }
        }

        public static string SerializeCustomProperties(object componentModel, bool useCamelCase = true)
        {
            var settings = new JsonSerializerSettings()
            {
                ContractResolver = new ComponentModelContractResolver(ComponentModelContractResolver.SerializationMode.CustomProperties),
                Converters = new List<JsonConverter>() { new IsoDateTimeConverter() }
            };
            return JsonConvert.SerializeObject(componentModel, Formatting.Indented, settings);
        }

        private static List<CustomPropertyInfo> GetCustomProperties(Type type)
        {
            var properties = type.GetProperties().Where(p =>
                typeof(ComponentModelBase).IsAssignableFrom(p.DeclaringType) && p.DeclaringType != typeof(ComponentModelBase)).ToList();

            return properties.Select(p =>
            {
                var propName = p.GetAttribute<JsonPropertyAttribute>()?.PropertyName;
                if (string.IsNullOrWhiteSpace(propName))
                {
                    var componentType = p.DeclaringType.GetAttribute<FormComponentPropsAttribute>()?.ComponentType;

                    propName = !string.IsNullOrWhiteSpace(componentType)
                        ? componentType + "-" + p.Name
                        : p.Name;
                }

                return new CustomPropertyInfo()
                {
                    Property = p,
                    PropertyName = propName
                };
            }).ToList();
        }

        private static readonly object _mappersLock = new Object();
        private static Dictionary<Type, IMapper> _componentModelMappers = new Dictionary<Type, IMapper>();

        private static IMapper GetMapper(ComponentInfo componentInfo)
        {
            if (_componentModelMappers.TryGetValue(componentInfo.ModelType, out var mapper))
                return mapper;

            lock (_mappersLock)
            {
                if (_componentModelMappers.TryGetValue(componentInfo.ModelType, out mapper))
                    return mapper;

                var mapperConfig = new MapperConfiguration(cfg =>
                {
                    cfg.CreateMap<FormComponent, ComponentModelBase>()
                        .IgnoreNotMapped()
                        .IgnoreAllSourcePropertiesWithAnInaccessibleSetter()
                        .IgnoreAllPropertiesWithAnInaccessibleSetter()
                        .IgnoreChildEntities()
                        .IgnoreChildEntityLists()
                        .IgnoreDestinationChildEntities()
                        .IgnoreDestinationChildEntityLists()
                        .ForMember(m => m.ComponentIds, e => e.MapFrom(c => ComponentService.GetAll().Where(i => i.Parent == c).OrderBy(i => i.SortIndex).Select(i => i.Id).ToList()));

                    var map = cfg.CreateMap(componentInfo.ModelType, componentInfo.ModelType);
                    foreach (var customProp in componentInfo.CustomProperties)
                    {
                        map.ForMember(customProp.Property.Name, a => a.Condition(o => true));
                    }
                    map.ForAllOtherMembers(a => a.Ignore());
                });
                mapper = mapperConfig.CreateMapper();
                _componentModelMappers[componentInfo.ModelType] = mapper;
                return mapper;
            }
        }

        public static ComponentModelBase GetComponentModel(FormComponent component, object model = null)
        {
            var componentInfo = GetComponentInfo(component.Type);

            var componentModel = Activator.CreateInstance(componentInfo.ModelType) as ComponentModelBase;

            var mapper = GetMapper(componentInfo);
            mapper.Map(component, componentModel);

            componentModel.IsInput = componentInfo.IsInput;

            if (!string.IsNullOrWhiteSpace(component.CustomSettings))
            {
                var settings = new JsonSerializerSettings()
                {
                    ContractResolver = new ComponentModelContractResolver(ComponentModelContractResolver.SerializationMode.CustomProperties),
                    Converters = new List<JsonConverter>() { new IsoDateTimeConverter() }
                };

                var customParams = JsonConvert.DeserializeObject(component.CustomSettings, componentInfo.ModelType, settings);

                mapper.Map(customParams, componentModel);
            }

            var instance = Activator.CreateInstance(componentInfo.ComponentType) as IFormComponentOld;
            componentModel.ContextData = instance?.GetContextData(componentModel, model);

            return componentModel;
        }

        /*
        public static T Deserialize<T>(string json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            var settings = new JsonSerializerSettings()
            {
                ContractResolver = new ConditionalContractResolver((o, p) => true),
                Converters = new List<JsonConverter>() { new IsoDateTimeConverter() }
            };
            return JsonConvert.DeserializeObject<T>(json, settings);
        }
        */

        public static string SerializeFull(object model)
        {
            var settings = new JsonSerializerSettings()
            {
                ContractResolver = new ComponentModelContractResolver(ComponentModelContractResolver.SerializationMode.AllProperties),
                Converters = new List<JsonConverter>() { new IsoDateTimeConverter() },
                Formatting = Formatting.Indented
            };

            return JsonConvert.SerializeObject(model, settings);
        }

        public static IQueryable<FormComponent> GetComponents(this Form form)
        {
            return ComponentService.GetAll().Where(c => c.Form == form);
        }

        public static IQueryable<FormComponent> GetChildComponents(this Form form, FormComponent component)
        {
            return ComponentService.GetAll().Where(c => c.Form == form && c.Parent == component).OrderBy(c => c.SortIndex).ThenBy(c => c.CreationTime);
        }

        public static void AutoGenerateForm(Form form, bool displayMode)
        {
            var modelType = !string.IsNullOrWhiteSpace(form.ModelType)
                ? Type.GetType(form.ModelType)
                : null;

            if (modelType == null)
                return;

            // inactivate all existing components
            var toDelete = form.GetComponents().ToList();
            foreach (var component in toDelete)
            {
                ComponentService.Delete(component);
            }

            var propertyGridConfig = new PropertyGridConfig(null)
            {
                ModelType = modelType,
                DefaultCategoryName = "Misc",
                CategoriesVisibility = c => !c.Equals("Audit", StringComparison.InvariantCultureIgnoreCase)
            };

            // skip lists
            propertyGridConfig.PropertiesVisibility = prop => !prop.PropertyType.IsSubtypeOfGeneric(typeof(IList<>));

            var mapper = GetDefaultMapper();

            var groups = PropertyGridDataProvider.GetPropertyGroups(propertyGridConfig, modelType, false);
            var panelIndex = 0;
            foreach (var group in groups)
            {
                var panelModel = new PanelModel()
                {
                    Type = "panel",
                    ApiKey = $"panel{panelIndex}",
                    Label = group.Category,
                    SortIndex = panelIndex
                };

                var componentModels = group.Properties.Select(p => GetComponentByPropertyConfig(p, displayMode)).Where(c => c != null).ToList();
                if (componentModels.Any())
                {
                    var panel = new FormComponent() { Form = form };
                    mapper.Map(panelModel, panel);
                    ComponentService.InsertOrUpdate(panel);
                    panelIndex++;

                    var componentIndex = 0;
                    foreach (var componentModel in componentModels)
                    {
                        componentModel.SortIndex = componentIndex++;
                        var component = new FormComponent()
                        {
                            Form = form,
                            Parent = panel
                        };
                        mapper.Map(componentModel, component);

                        component.CustomSettings = SerializeCustomProperties(componentModel);

                        ComponentService.InsertOrUpdate(component);
                    }
                }
            }
        }

        private static IMapper GetDefaultMapper()
        {
            return new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<ComponentModelBase, ComponentModelBase>();
                cfg.CreateMap<ComponentModelBase, FormComponent>();
                cfg.CreateMap<FormComponent, ComponentModelBase>();
            }).CreateMapper();
        }

        private static T GetComponentWithStandardSettings<T>(ModelPropertyConfig config) where T : ComponentModelBase, new()
        {
            var type = Components.Values.FirstOrDefault(c => c.ModelType == typeof(T))?.Type;
            return new T
            {
                Type = type,
                ApiKey = config.Path,
                Label = config.Label,
                Description = config.Description,
                Disabled = config.Readonly,
                Required = config.Required,
                SortIndex = config.OrderIndex
            };
        }

        public static ComponentModelBase GetComponentByPropertyConfig(ModelPropertyConfig propertyConfig, bool displayMode)
        {
            if (displayMode)
                return GetComponentWithStandardSettings<DisplayTextModel>(propertyConfig);

            switch (propertyConfig.DataType)
            {
                case GeneralDataType.Boolean:
                    {
                        return GetComponentWithStandardSettings<CheckboxModel>(propertyConfig);
                    }
                case GeneralDataType.DateTime:
                case GeneralDataType.Date:
                    {
                        return GetComponentWithStandardSettings<DateTimePickerModel>(propertyConfig);
                    }
                case GeneralDataType.EntityReference:
                    {
                        var autocomplete = GetComponentWithStandardSettings<AutocompleteModel>(propertyConfig);
                        autocomplete.DataSourceType = AutocompleteDataSourceType.EntitiesList;
                        autocomplete.EntityTypeShortAlias = propertyConfig.EntityTypeShortAlias;
                        return autocomplete;
                    }
                case GeneralDataType.ReferenceList:
                    {
                        var autocomplete = GetComponentWithStandardSettings<SelectModel>(propertyConfig);
                        autocomplete.DataSourceType = SelectDataSourceType.ReferenceList;
                        autocomplete.ReferenceListName = propertyConfig.ReferenceListName;
                        autocomplete.ReferenceListNamespace = propertyConfig.ReferenceListNamespace;
                        return autocomplete;
                    }
                case GeneralDataType.Numeric:
                    {
                        var numeric = GetComponentWithStandardSettings<NumberFieldModel>(propertyConfig);
                        return numeric;
                    }
                case GeneralDataType.Text:
                    {
                        var textField = GetComponentWithStandardSettings<TextFieldModel>(propertyConfig);
                        return textField;
                    }
            }

            return null;
        }

        public static Dictionary<string, ComponentModelBase> ToTreeWithFakeRoot(this List<ComponentModelBase> components)
        {
            // In case duplicated IDs are found on the form, return them in exception text for easier tracking of the root cause.
            var duplicatedIds = components.GroupBy(c => c.Id, (id, c) => new { Id = id, Count = c.Count() })
                .Where(c => c.Count > 1)
                .ToList();
            if (duplicatedIds.Any())
                throw new Exception("Duplicated IDs on form: " +
                                    string.Join("; ", duplicatedIds.Select(d => d.Id + " - x" + d.Count)));

            var result = components.ToDictionary(i => i.Id.ToString(), i => i);

            result.Add("", new ComponentModelBase()
            {
                ComponentIds = components.Where(c => c.ParentId == null && c.Id != null).Select(c => c.Id.Value).ToList()
            });

            return result;
        }

        public static List<ComponentModelBase> GetFormComponents(Form form, object model = null, bool bindSubforms = true, Guid? containerId = null)
        {
            var components = DoGetFormComponents(form, model, containerId);

            if (bindSubforms)
            {
                // add subform components recursively
                var subForms = components.Where(c => c.Type == SubFormComponent.Type).Cast<SubFormModel>().ToList();
                foreach (var subForm in subForms)
                {
                    var subFormRoot = subForm.FormId.HasValue
                        ? _formService.Get(subForm.FormId.Value)
                        : null;

                    if (subFormRoot != null)
                    {
                        var subFormComponents = GetFormComponents(subFormRoot, model);

                        var rootComponents = subFormComponents.Where(c => c.ParentId == null).ToList();
                        foreach (var component in rootComponents)
                        {
                            component.ParentId = subForm.Id;
                            subForm.ComponentIds.Add(component.Id.Value);
                        }

                        // temporary fix for subforms which may contain the same components
                        foreach (var component in subFormComponents)
                        {
                            if (!components.Any(c => c.Id == component.Id))
                                components.AddRange(subFormComponents);
                            else
                            {

                            }
                        }
                    }
                }
            }

            return components;
        }

        private static List<ComponentModelBase> DoGetFormComponents(Form form, object model = null, Guid? containerId = null)
        {
            var allComponents = ComponentService.GetAll().Where(c => c.Form == form)
                .OrderBy(c => c.SortIndex)
                .ThenBy(c => c.CreationTime)
                .ToList();

            // note: temporary fix
            allComponents = allComponents
                .Where(c => c.Parent == null || c.GetFullChain(cc => cc.Parent).All(p => !p.IsDeleted)).ToList();

            if (containerId.HasValue)
                allComponents = allComponents.Where(c => c.Id == containerId || c.Closest(cc => cc.Parent, cc => cc.Parent?.Id == containerId) != null).ToList();

            var components = allComponents
                .Select(c => GetComponentModel(c, model))
                .ToList();

            return components;
        }

        /// <summary>
        /// Returns component models of the specified form which which should be bound (has data)
        /// </summary>
        public static Dictionary<string, ComponentModelBase> GetInputModels(Form form, Guid? containerId = null, object model = null)
        {
            var inputTypes = Components.Where(c => c.Value.IsInput).Select(c => c.Value.Type).ToList();

            //var visibleComponentsHeader = HttpContext.Current.Request.Headers[VisibleComponentsHeader];
            var visibleComponentsHeader = "";
            var visibleComponents = (visibleComponentsHeader ?? "")
                .Split(',', ';')
                .Select(i => i.ToGuid())
                .Where(i => i != Guid.Empty)
                .ToList();

            var allComponents = GetFormComponents(form, model, containerId: containerId)
                .Where(c => !string.IsNullOrEmpty(c.ApiKey)
                            && inputTypes.Contains(c.Type)
                            && (visibleComponentsHeader == null || visibleComponents.Contains(c.Id.GetValueOrDefault())) /*todo: analyze visible components before fetching of the models*/)
                .ToList();

            var components = allComponents
                .ToDictionary(i => i.Id.ToString(), i => i);

            return components;
        }

        public class ComponentInfo
        {
            public string Type { get; set; }
            public bool IsInput { get; set; }
            public bool IsOutput { get; set; }
            public string JavascriptLibrary { get; set; }
            public bool BindChildProps { get; set; }
            public Type ModelType { get; set; }
            public Type ComponentType { get; set; }
            public List<CustomPropertyInfo> CustomProperties { get; set; }

            public ComponentInfo()
            {
                CustomProperties = new List<CustomPropertyInfo>();
            }
        }

        public class CustomPropertyInfo
        {
            public PropertyInfo Property { get; set; }
            /// <summary>
            /// Property Name as it's posted to the server
            /// </summary>
            public string PropertyName { get; set; }
        }

        private static JsonSerializerSettings GetJsonSerializerSettings()
        {
            var modelTypes = Components.ToDictionary(c => c.Value.Type, c => c.Value.ModelType);
            return new JsonSerializerSettings()
            {
                Converters = new List<JsonConverter>()
                {
                    new IsoDateTimeConverter(),
                    new ComponentModelJsonConverter(modelTypes)
                }
            };
        }

        /*
        public static void ImportFormsJson(string json)
        {
            var forms = JsonConvert.DeserializeObject<List<FormModel>>(json, GetJsonSerializerSettings());
            var service = StaticContext.IocManager.Resolve<IRepository<Form, Guid>>();
            foreach (var formToImport in forms)
            {
                var form = service.Get(formToImport.Id, false, includeInactive: true);
                if (form == null)
                {
                    form = new Form();
                    form.SetId(formToImport.Id);
                }
                else
                {
                    if (form.InactiveFlag)
                        form.Activate();
                    CleanForm(form, "Removed during the form import");
                }

                form.Name = formToImport.Name;
                form.Description = formToImport.Description;
                form.ModelType = formToImport.ModelType;
                service.SaveOrUpdate(form);

                ImportChildComponents(form, formToImport.Components, null, null);
            }
        }
        */

        /// <summary>
        /// Inactivates all existing components of the form
        /// </summary>
        /// <param name="form"></param>
        /// <param name="comment"></param>
        public static void CleanForm(Form form, string comment)
        {
            var oldComponents = ComponentService.GetAll().Where(c => c.Form == form).ToList();
            foreach (var formComponent in oldComponents)
            {
                ComponentService.Delete(formComponent);
            }
            //NHibernateSession.Current.Flush();
        }

        public static void ImportFormJson(Form form, string json)
        {
            var components = JsonConvert.DeserializeObject<List<ComponentModelBase>>(json, GetJsonSerializerSettings());

            // import components recursively
            ImportChildComponents(form, components, null, null);

            //NHibernateSession.Current.Flush();
        }

        private static void ImportChildComponents(Form form, List<ComponentModelBase> components, Guid? oldParentId, FormComponent newParent)
        {
            var componentService = StaticContext.IocManager.Resolve<IRepository<FormComponent, Guid>>();
            var componentsOnLevel = components.Where(c => c.ParentId == oldParentId).ToList();

            var mapper = new MapperConfiguration(cfg => { cfg.CreateMap<ComponentModelBase, ComponentModelBase>(); }).CreateMapper();

            foreach (var componentModel in componentsOnLevel)
            {
                var formComponent = new FormComponent();

                mapper.Map(componentModel, formComponent);

                formComponent.Form = form;
                formComponent.Parent = newParent;
                formComponent.Id = Guid.NewGuid();

                // save custom properties
                formComponent.CustomSettings = SerializeCustomProperties(componentModel);

                componentService.InsertOrUpdate(formComponent);

                if (componentModel.Type != SubFormComponent.Type)
                    ImportChildComponents(form, components, componentModel.Id, formComponent);
            }
        }

        public static void BindFormToModel<TModel>(Form form, TModel model, Guid? bindContainerId,
            ModelStateDictionary modelState)
        {
            BindFormToModel(form, model, bindContainerId, (componentModel, error) =>
            {
                modelState.AddModelError(componentModel.ApiKey, error);
            });
        }

        public static void BindFormToModel<TModel>(Form form, TModel model, Guid? bindContainerId, Action<ComponentModelBase, string> errorHandler)
        {
            if (form == null)
                return;

            var components = GetInputModels(form, bindContainerId);

            foreach (var component in components)
            {
                var componentModel = component.Value;

                var instance = GetComponentInstance(componentModel.Type);
                if (instance != null && instance.ShouldBeBound(componentModel))
                {
                    instance.BindModel(componentModel, model, out List<string> errors);
                    if (errors != null)
                    {
                        foreach (var error in errors)
                        {
                            errorHandler?.Invoke(componentModel, error);
                        }
                    }
                }
            }
        }

        public static List<string> GetComponentScripts(this UrlHelper urlHelper, List<ComponentModelBase> componentModels)
        {
            return componentModels.Select(c => c.Type).Distinct()
                .Select(t => FormDesignerHelper.Components[t]?.JavascriptLibrary)
                .Where(j => !string.IsNullOrWhiteSpace(j))
                .Select(j => j.StartsWith("~")
                    ? urlHelper.Content(j)
                    : j)
                .Distinct()
                .ToList();
        }

        public static string GetJsonModel<T>(List<ComponentModelBase> components, T model)
        {
            var jModel = new JObject();

            var componentsToBind = components
                .Select(c =>
                {
                    var componentInfo = FormDesignerHelper.GetComponentInfo(c.Type);
                    return new
                    {
                        ComponentInfo = componentInfo,
                        ComponentModel = c,
                        DotsCount = c.ApiKey.CharCount('.')
                    };
                })
                .Where(c => (c.ComponentInfo.IsInput || c.ComponentInfo.IsOutput) && !string.IsNullOrWhiteSpace(c.ComponentModel.ApiKey))
                .OrderBy(c => c.DotsCount)
                .ToList();

            foreach (var component in componentsToBind)
            {
                var componentModel = component.ComponentModel;

                if (jModel.Properties().Any(p => p.Name == componentModel.ApiKey))
                    continue;

                var instance = Activator.CreateInstance(component.ComponentInfo.ComponentType) as IFormComponentOld;
                if (instance != null)
                {
                    var componentData = instance.GetModel(componentModel, model);

                    AddProperty(jModel, componentModel.ApiKey, componentData);
                }
            }

            // include first level of model irrespectively of components tree
            var missingProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(mp => mp.CanRead /*&& mp.CanWrite*/ && !jModel.Properties().Any(p => p.Name == mp.Name))
                .ToList();

            foreach (var prop in missingProperties)
            {
                var config = prop.GetHardCodedConfig(useCamelCase: false);
                var propValue = prop.GetValue(model);

                if (config.DataType == GeneralDataType.List)
                    continue;

                var value = propValue == null
                    ? null
                    : propValue is IEntity entity
                        ? entity.GetId()
                        : propValue;

                AddProperty(jModel, config.Path, value);
            }

            return jModel.ToString();
        }

        private static void AddProperty(JObject source, string key, object value)
        {
            var owner = source;

            var parts = key.Split('.');
            if (parts.Length > 1)
            {
                var currentOwner = owner;
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    if (currentOwner[parts[i]] != null && !(currentOwner[parts[i]] is JObject))
                        currentOwner[parts[i]] = new JObject();

                    var newOwner = currentOwner[parts[i]] as JObject;

                    if (newOwner == null)
                    {
                        newOwner = new JObject();
                        currentOwner.Add(parts[i], newOwner);
                    }
                    currentOwner = newOwner;

                }
                owner = currentOwner;
                key = parts[parts.Length - 1];
            }

            JToken token = null;

            if (value is string)
                token = (string)value;
            else if (value is IList)
                token = JArray.FromObject(value);
            else
            if (value is IEntity)
            {
                var entity = (value as IEntity);
                /*
                var entityDesc = new
                {
                    Id = entity.GetId(),
                    DisplayName = entity.GetDisplayName()
                };
                token = JToken.FromObject(entityDesc);
                */
                token = entity.GetId().ToString();
            }
            else
                token = value != null
                        ? JToken.FromObject(value)
                        : (JToken)null;

            var existingProp = owner.Property(key);
            if (existingProp != null)
            {
                var existing = existingProp.Value as JObject;
                existing?.Merge(token, new JsonMergeSettings
                {
                    // union array values together to avoid duplicates
                    MergeArrayHandling = MergeArrayHandling.Union
                });
            }
            else
                owner.Add(key, token);
        }

    }

}

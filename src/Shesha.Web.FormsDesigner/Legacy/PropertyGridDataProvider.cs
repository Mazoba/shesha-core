using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Hosting.Internal;
using Shesha.Domain.Attributes;
using Shesha.Reflection;

namespace Shesha.Web.FormsDesigner.Legacy
{
    public class PropertyGridDataProvider : IPropertyGridDataProvider
    {
        public object Model { get; protected set; }

        public PropertyGridDataProvider(object model)
        {
            Model = model;
        }

        public List<CategoryInfoModel> GetGroups(PropertyGridConfig config)
        {
            var modelType = Model.GetType().StripCastleProxyType();

            var propertyGroups = GetPropertyGroups(config, modelType, false);

            var customCategoriesAttributes = GetCustomCategoriesAttributes(modelType, config.HideInherited);
            foreach (var customCategoriesAttribute in customCategoriesAttributes)
            {
                if (!string.IsNullOrWhiteSpace(customCategoriesAttribute.ScanCategoriesPath))
                {
                    /*
                    var categoriesPath = Path.Combine(customCategoriesAttribute.ScanCategoriesPath,
                        config.Readonly ? "DisplayCategories" : "EditCategories").Replace('\\', '/');

                    var virtualDir = HostingEnvironment.VirtualPathProvider.GetDirectory(categoriesPath);

                    var files = virtualDir.Files.Cast<VirtualFileBase>();
                    foreach (var file in files)
                    {
                        var categoryName = Path.GetFileNameWithoutExtension(file.Name).ToFriendlyName();
                        if (config.HiddenCategories.Contains(categoryName))
                            continue;

                        var category = propertyGroups.FirstOrDefault(g => g.Category.Equals(categoryName, StringComparison.InvariantCultureIgnoreCase));
                        if (category == null)
                        {
                            category = new CategoryInfoModel
                            {
                                Category = categoryName
                            };
                            propertyGroups.Add(category);
                        }
                        if (!category.CustomViews.Contains(file.VirtualPath, StringComparer.InvariantCultureIgnoreCase))
                            category.CustomViews.Add(file.VirtualPath);
                    }
                    */
                }
            }

            foreach (var editor in config.CustomEditors)
            {
                var category = propertyGroups.FirstOrDefault(g => g.Category.Equals(editor.Category, StringComparison.InvariantCultureIgnoreCase));
                if (category == null)
                {
                    category = new CategoryInfoModel
                    {
                        Category = editor.Category
                    };
                    propertyGroups.Add(category);
                }

                if (!category.CustomViews.Contains(editor.PartialViewName, StringComparer.InvariantCultureIgnoreCase))
                    category.CustomViews.Add(editor.PartialViewName);
            }

            if (config.CategoriesVisibility != null)
                propertyGroups = propertyGroups.Where(g => config.CategoriesVisibility.Invoke(g.Category)).ToList();

            propertyGroups = propertyGroups
                .OrderBy(g => g.Category.Equals("Uncategorized") ? int.MaxValue : g.OrderIndex)
                .ThenBy(g => g.Category)
                .ToList();

            return propertyGroups;
        }

        public static List<CategoryInfoModel> GetPropertyGroups(PropertyGridConfig config, Type modelType, bool useCamelCase)
        {
            var flags = BindingFlags.Public | BindingFlags.Instance;
            if (config.HideInherited)
                flags = flags | BindingFlags.DeclaredOnly;

            var groups = modelType.GetProperties(flags)
                .Where(p =>
                    p.CanRead && p.CanWrite &&
                    !config.HiddenProperties.Contains(p.Name) &&
                    (p.GetAttribute<BrowsableAttribute>()?.Browsable ?? true) == true &&
                    (!config.ShowOnlyCategorized || !string.IsNullOrWhiteSpace(GetCategoryName(p, config.ModelType))) &&
                    (config.PropertiesVisibility?.Invoke(p) ?? true)
                )
                .GroupBy(p => GetCategoryName(p, config.ModelType) ?? config.DefaultCategoryName, p => p, (c, props) =>
                    new CategoryInfoModel
                    {
                        Category = c,
                        OrderIndex = config.CategoriesOrderFunc?.Invoke(c) ?? 0,
                        Properties = props.Select(p => p.GetHardCodedConfig(config.ModelType, useCamelCase)).ToList()
                    })
                .Where(g => !config.HiddenCategories.Contains(g.Category))
                .ToList();

            if (config.CategoriesVisibility != null)
                groups = groups.Where(g => config.CategoriesVisibility.Invoke(g.Category)).ToList();

            return groups;
        }

        internal static List<CustomCategoriesAttribute> GetCustomCategoriesAttributes(Type type, bool hideInherited)
        {
            var result = new List<CustomCategoriesAttribute>();

            var currentType = type;
            while (currentType != null)
            {
                var attribute = currentType.GetAttribute<CustomCategoriesAttribute>();
                if (attribute != null)
                    result.Add(attribute);

                currentType = hideInherited
                    ? null
                    : currentType.BaseType;
            }

            return result;
        }

        internal static string GetCategoryNameFromCurrentType(PropertyInfo propertyInfo)
        {
            // 1. search the CategoryAttribute on the property 
            var ownCategoryAttribute = propertyInfo.GetAttribute<CategoryAttribute>(true);
            if (!string.IsNullOrWhiteSpace(ownCategoryAttribute?.Category))
                return ownCategoryAttribute.Category;

            // 2. search the DisplayAttribute with non empty GroupName on the property 
            var ownGroup = propertyInfo.GetAttribute<DisplayAttribute>(true)?.GroupName;
            if (!string.IsNullOrWhiteSpace(ownGroup))
                return ownGroup;

            return null;
        }

        internal static string GetCategoryName(PropertyInfo propertyInfo, Type declaredType)
        {
            var category = GetCategoryNameFromCurrentType(propertyInfo);
            if (!string.IsNullOrEmpty(category))
                return category;

            // try to search in the declared type if it differs from the propertyInfo.DeclaringType
            if (propertyInfo.DeclaringType != declaredType && propertyInfo.DeclaringType != null)
            {
                var declaredProperty = declaredType.GetProperty(propertyInfo.Name);
                category = declaredProperty != null
                    ? GetCategoryNameFromCurrentType(declaredProperty)
                    : null;
                if (!string.IsNullOrWhiteSpace(category))
                    return category;

                var interfaces = propertyInfo.DeclaringType.GetInterfaces();
                foreach (var @interface in interfaces)
                {
                    declaredProperty = @interface.GetProperty(propertyInfo.Name);
                    category = declaredProperty != null
                        ? GetCategoryNameFromCurrentType(declaredProperty)
                        : null;

                    if (!string.IsNullOrWhiteSpace(category))
                        return category;
                }

            }

            // if nothing found - try to get category from the declaring type (owner of the property)
            return propertyInfo.DeclaringType.GetAttribute<CategoryAttribute>(true)?.Category;
        }

        public IHtmlContent RenderDisplay(HtmlHelper htmlHelper, ModelPropertyConfig property)
        {
            return htmlHelper.Display(property.Path, null, null, Model);
        }

        public IHtmlContent RenderEdit(HtmlHelper htmlHelper, ModelPropertyConfig property)
        {
            return htmlHelper.Editor(property.Path, null, null, Model);
        }
    }

}

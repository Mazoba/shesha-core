using System;
using Shesha.AutoMapper.Dto;
using Shesha.Domain;
using Shesha.Extensions;
using Shesha.Reflection;
using Shesha.Services;

namespace Shesha.Web.DataTable.Columns
{
    /// <summary>
    /// Display property column
    /// </summary>
    public class DataTablesDisplayPropertyColumn: DataTableColumn
    {
        /// inheritedDoc
        public override object CellContent(object entity)
        {
            
            switch (DataType)
            {
                case ColumnDataTypes.DateTime:
                case ColumnDataTypes.Date:
                case ColumnDataTypes.Time:
                case ColumnDataTypes.Boolean:
                {
                    var value = ReflectionHelper.GetPropertyValue(entity, PropertyName, out object parentEntity, out var propInfo);
                    return value;
                }
                case ColumnDataTypes.MultiValueReferenceList:
                    return entity.GetMultiValueReferenceListItemNames(PropertyName, "");
                default: 
                    return string.IsNullOrEmpty(PropertyName)
                        ? ""
                        : GetPropertyValue(entity, PropertyName);
            }
        }

        private object GetPropertyValue(object entity, string propertyName, string defaultValue = "")
        {
            try
            {
                var val = ReflectionHelper.GetPropertyValue(entity, propertyName, out object parentEntity, out var propInfo);
                if (val == null)
                    return defaultValue;

                var propConfig = parentEntity.GetType().GetEntityConfiguration()[propInfo.Name];
                var generalDataType = propConfig.GeneralType;

                entity = parentEntity;
                propertyName = propInfo.Name;

                switch (generalDataType)
                {
                    case Configuration.Runtime.GeneralDataType.Enum:
                    {
                        var itemValue = Convert.ToInt64(val);
                        return ReflectionHelper.GetEnumDescription(propConfig.EnumType, itemValue);
                    }
                    case Configuration.Runtime.GeneralDataType.ReferenceList:
                        {
                            var refListHelper = StaticContext.IocManager.Resolve<IReferenceListHelper>();
                            var itemValue = Convert.ToInt64(val);
                            var displayText = refListHelper.GetItemDisplayText(propConfig.ReferenceListNamespace, propConfig.ReferenceListName, itemValue);

                            if (!DataTableConfig.UseDtos)
                                return displayText;

                            var dto = new ReferenceListItemValueDto
                            {
                                Item = displayText,
                                ItemValue = itemValue
                            };
                            return dto;
                        }
                    case Configuration.Runtime.GeneralDataType.EntityReference:
                        {
                            var displayProperty = val.GetType().GetEntityConfiguration().DisplayNamePropertyInfo;
                            var displayText = displayProperty != null
                                ? displayProperty.GetValue(val)?.ToString()
                                : val.ToString();

                            if (!DataTableConfig.UseDtos)
                                return displayText;

                            var dto = new EntityWithDisplayNameDto<string>(val.GetId().ToString(), displayText);
                            return dto;
                        }
                    default:
                        return EntityExtensions.GetPrimitiveTypePropertyDisplayText(val, propInfo, defaultValue);
                }
                // todo: review and remove
                /*
                var propConfig = EntityConfiguration.Get(parentEntity.GetType())[propInfo.Name];
                var generalDataType = propConfig.GeneralType;

                entity = parentEntity;
                propertyName = propInfo.Name;

                switch (generalDataType)
                {
                    case GeneralDataType.Enum:
                        var itemValue = (int)val;
                        return ReflectionHelper.GetEnumDescription(propConfig.EnumType, itemValue);
                    case GeneralDataType.ReferenceList:
                        return entity.GetReferenceListItemName(propertyName, defaultValue);
                    case GeneralDataType.MultiValueReferenceList:
                        return entity.GetMultiValueReferenceListItemNames(propertyName, defaultValue);
                    case GeneralDataType.EntityReference:
                    case GeneralDataType.StoredFile:
                        var referencedEntity = entity.GetReferencedEntity(propertyName);

                        if (referencedEntity == null)
                            return defaultValue ?? "";
                        else
                            return referencedEntity.GetDisplayName();
                    default:
                        return GetPrimitiveTypePropertyDisplayText(val, propInfo, defaultValue);
                }
                */
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"An error occured whilst trying to retrieve DisplayText of property '{propertyName}' on type of '{entity.GetType().FullName}'.", ex);
            }
        }

        internal DataTablesDisplayPropertyColumn(DataTableConfig dataTableConfig) : base(dataTableConfig)
        {
        }
    }
}

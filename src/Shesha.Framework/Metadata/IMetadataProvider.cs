using Shesha.Metadata.Dtos;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.Metadata
{
    /// <summary>
    /// Metadata provider
    /// </summary>
    public interface IMetadataProvider
    {
        /// <summary>
        /// Get metadata of specified property
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        PropertyMetadataDto GetPropertyMetadata(PropertyInfo property);

        /// <summary>
        /// Get properties metadata of the specified <paramref name="type"/>
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        List<PropertyMetadataDto> GetProperties(Type type);

        /// <summary>
        /// Get data type of the specified property
        /// </summary>
        /// <returns></returns>
        string GetDataType(PropertyInfo propInfo);
    }
}

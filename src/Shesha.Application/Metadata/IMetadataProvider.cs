using Shesha.Metadata.Dtos;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

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
    }
}

using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Newtonsoft.Json;
using System;
using System.Text.Json.Serialization;

namespace Shesha.Metadata.Dtos
{
    public class PropertyMetadataDto
    {
        public bool IsVisible { get; set; }
        public bool Required { get; set; }
        public bool Readonly { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }

        public double? Min { get; set; }
        public double? Max { get; set; }

        public string Path { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }

        public string DataType { get; set; }
        public string DataFormat { get; set; }

        [JsonProperty("entityType")]
        [JsonPropertyName("entityType")]
        public string EntityTypeShortAlias { get; set; }
        
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public Type EnumType { get; set; }

        public string ReferenceListName { get; set; }
        public string ReferenceListNamespace { get; set; }

        public int OrderIndex { get; set; }
        public string GroupName { get; set; }

        /// <summary>
        /// If true, indicates that current property is a framework-related (e.g. <see cref="ISoftDelete.IsDeleted"/>, <see cref="IHasModificationTime.LastModificationTime"/>)
        /// </summary>
        public bool IsFrameworkRelated { get; set; }
    }
}

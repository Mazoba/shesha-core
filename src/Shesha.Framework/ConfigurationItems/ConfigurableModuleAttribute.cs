using Shesha.Utilities;
using System;

namespace Shesha.ConfigurationItems
{
    /// <summary>
    /// Configurable module attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ConfigurableModuleAttribute: Attribute
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public ConfigurableModuleAttribute(string id, string name)
        {
            Id = id.ToGuid();
            if (Id == Guid.Empty)
                throw new NotSupportedException($"Id of the module must be a valid Guid");

            Name = name;
        }
        public ConfigurableModuleAttribute(string id, string name, string description): this(id, name)
        {
            Description = description;
        }
    }
}

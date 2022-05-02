using System;

namespace Shesha.Configuration.Runtime.Exceptions
{
    public class EntityTypeNotFound: Exception
    {
        public string ClassNameOrAlias { get; set; }

        public EntityTypeNotFound(string classNameOrAlias): base($"Entity with class name or alias '{classNameOrAlias}' not found")
        {
            ClassNameOrAlias = classNameOrAlias;
        }
    }
}

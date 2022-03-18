using System;
using System.Collections.Generic;
using System.Linq;

namespace Shesha.Permissions.Attributes
{
    public class ProtectedAttribute: Attribute
    {
        public string Description { get; set; }

        public List<ProtectedAction> Actions { get; set; }

        public ProtectedAttribute(string description)
        {
            Description = description;
            Actions = new List<ProtectedAction>();
        }

        public ProtectedAttribute(string description, params string[] actions)
        {
            Description = description;
            //Actions = actions?.ToList() ?? new List<ProtectedAction>();
        }
    }

    public class ProtectedAction
    {
        public string Action { get; set; }
        public string Description { get; set; }
    }
}
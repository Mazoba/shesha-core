using System.Collections.Generic;

namespace Shesha.Permissions
{
    public class ProtectedObject
    {
        public string Object { get; set; }
        public string Action { get; set; }

        public string Description { get; set; }

        public List<string> Permissions { get; set; } = new List<string>();

        public bool IsInherited { get; set; } = true;

        public ProtectedObject Parent { get; set; } = null;
        public List<ProtectedObject> Child { get; set; } = new List<ProtectedObject>();
    }
}
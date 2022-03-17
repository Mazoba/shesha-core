using System.Collections.Generic;

namespace Shesha.Permissions
{
    public class ApiPermission
    {
        public string Class { get; set; }
        public string Method { get; set; }

        public string Description { get; set; }

        public List<string> Permissions { get; set; } = new List<string>();

        public bool IsInherited { get; set; } = true;

        public ApiPermission Parent { get; set; } = null;
        public List<ApiPermission> Child { get; set; } = new List<ApiPermission>();
    }
}
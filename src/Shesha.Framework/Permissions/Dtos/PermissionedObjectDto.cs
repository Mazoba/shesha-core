using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Application.Services.Dto;
using ConcurrentCollections;
using Shesha.Domain.Enums;

namespace Shesha.Permissions
{
    public class PermissionedObjectDto : EntityDto<Guid>
    {

        public const string CacheStoreName = "PermissionedObjectCache";

        public PermissionedObjectDto()
        {
            Permissions = new ConcurrentHashSet<string>();
            Child = new List<PermissionedObjectDto>();
            Access = (int)RefListPermissionedAccess.Inherited;
            Hidden = false;
            AdditionalParameters = new Dictionary<string, string>();
        }

        public string Object { get; set; }

        public string Category { get; set; }

        public string Module { get; set; }

        public string Type { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public ConcurrentHashSet<string> Permissions { get; set; }

        public ConcurrentHashSet<string> ActualPermissions { get; set; }

        public int? Access { get; set; }

        public bool Inherited => Access == (int) RefListPermissionedAccess.Inherited;
        public int? ActualAccess { get; set; }

        public string Parent { get; set; }
        public string Dependency { get; set; }
        
        public List<PermissionedObjectDto> Child { get; set; }

        public bool Hidden { get; set; }

        public Dictionary<string, string> AdditionalParameters { get; set; }

        public override string ToString()
        {
            var permissions = Hidden 
                ? "Hidden" 
                : Access == (int)RefListPermissionedAccess.RequiresPermissions
                    ? string.Join(", ", Permissions)
                : Access.ToString();
            return $"{Object} -> {Dependency} ({permissions})";
        }
    }
}
using System;
using System.Collections.Generic;
using Abp.Application.Services.Dto;
using ConcurrentCollections;

namespace Shesha.Permissions
{
    public class ProtectedObjectDto : EntityDto<Guid>
    {

        public const string CacheStoreName = "ProtectedObjectCache";

        public ProtectedObjectDto()
        {
            Permissions = new ConcurrentHashSet<string>();
            Child = new List<ProtectedObjectDto>();
            Inherited = true;
            Hidden = false;
        }

        public string Object { get; set; }

        public string Category { get; set; }

        public string Description { get; set; }

        public ConcurrentHashSet<string> Permissions { get; set; }

        public bool Inherited { get; set; }

        public string Parent { get; set; }
        public string Dependency { get; set; }
        
        public List<ProtectedObjectDto> Child { get; set; }

        public bool Hidden { get; set; }

        public override string ToString()
        {
            var permissions = Hidden ? "Hidden" : Inherited ? "Inherited" : string.Join(", ", Permissions);
            return $"{Object} -> {Dependency} ({permissions})";
        }
    }
}
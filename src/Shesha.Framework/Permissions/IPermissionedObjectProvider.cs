using System;
using System.Collections.Generic;

namespace Shesha.Permissions
{
    public interface IPermissionedObjectProvider
    {
        string GetObjectType();
        string GetObjectType(Type type);
        List<PermissionedObjectDto> GetAll();
    }
}
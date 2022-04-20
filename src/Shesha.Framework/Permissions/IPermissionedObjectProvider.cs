using System;
using System.Collections.Generic;

namespace Shesha.Permissions
{
    public interface IPermissionedObjectProvider
    {
        string GetCategory();
        string GetCategoryByType(Type type);
        List<PermissionedObjectDto> GetAll();
    }
}
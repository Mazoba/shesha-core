using System;
using System.Collections.Generic;

namespace Shesha.Permissions
{
    public interface IProtectedObjectProvider
    {
        string GetCategory();
        string GetCategoryByType(Type type);
        List<ProtectedObjectDto> GetAll();
    }
}
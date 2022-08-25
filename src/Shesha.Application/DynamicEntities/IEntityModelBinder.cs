using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.DynamicEntities
{
    public interface IEntityModelBinder
    {
        bool BindProperties(JObject jobject, object entity, List<ValidationResult> validationResult,
            string propertyName = null);
    }
}

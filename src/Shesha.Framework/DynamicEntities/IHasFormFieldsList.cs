using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.DynamicEntities
{
    /// <summary>
    /// Declares common interface for DTOs those have form fields list. Is used for dynamic entities
    /// </summary>
    public interface IHasFormFieldsList
    {
        public List<string> _formFields { get; set; }
    }
}

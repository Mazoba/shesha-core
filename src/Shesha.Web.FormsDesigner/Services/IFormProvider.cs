using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Shesha.Web.FormsDesigner.Dtos;

namespace Shesha.Web.FormsDesigner.Services
{
    public interface IFormProvider
    {
        Task<IList<FormDtoOld>> GetForms();
    }
}

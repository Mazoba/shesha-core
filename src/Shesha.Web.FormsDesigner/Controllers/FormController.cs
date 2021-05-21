using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Shesha.Web.FormsDesigner.Services;

namespace Shesha.Web.FormsDesigner.Controllers
{
    /*
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class FormController: ControllerBase, ITransientDependency
    {
        private readonly IFormProvider _formProvider;

        public FormController(IFormProvider formProvider)
        {
            _formProvider = formProvider;
        }

        [HttpGet]
        public async Task<IList<FormDto>> List()
        {
            var debug = false;
            if (debug)
                throw new Exception("Exception from List");
            return await _formProvider.GetForms();
        }

        [HttpGet]
        [DontWrapResult]
        public async Task<IList<FormDto>> List2()
        {
            var debug = false;
            if (debug)
                throw new Exception("Exception from List2");
            return await _formProvider.GetForms();
        }
    }
    */
}

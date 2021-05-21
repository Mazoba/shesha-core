using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abp.Dependency;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Shesha.Web.FormsDesigner.Dtos;

namespace Shesha.Web.FormsDesigner.Services
{
    public class FormProvider: IFormProvider, ITransientDependency
    {
        private string _formsRootPath = "/Forms";
        private readonly IWebHostEnvironment _hostEnvironment;
        public FormProvider(IWebHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }

        public Task<IList<FormDtoOld>> GetForms()
        {
            var fileProvider = _hostEnvironment.ContentRootFileProvider;
            var formsFolder = fileProvider.GetDirectoryContents(_formsRootPath);

            var forms = new List<FormDtoOld>();

            /*
public interface IDirectoryContents : IEnumerable<IFileInfo>, IEnumerable
  {
    /// <summary>True if a directory was located at the given path.</summary>
    bool Exists { get; }
  }             
             *
             */

            // todo: 1. make a list of forms in a separate json file as a cache with MD5 or date of last modification
            // todo: 2. parse base properties and save them to the cached list
            // todo: 3. on each request check the modification date and compare with value from the cache
            if (formsFolder.Exists)
            {
                /*
                Name
                Description
                ModelType
                 */
                var folderForms = formsFolder.Select(f => new FormDtoOld()
                {
                    Id = f.Name
                }).ToList();
                forms.AddRange(folderForms);
            }
            
            return Task.FromResult((IList<FormDtoOld>)forms);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.Applications
{
    /// <summary>
    /// A Front-end App represents the various front-end applications that build off this back-end. 
    /// For example, Admin Portal, Customer Portal, Customer Mobile App would be fairly typical examples.
    /// </summary>
    public class FrontEndApp
    {
        /// <summary>
        /// Name of the front-end app
        /// </summary>
        public virtual string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public virtual string Description { get; set; }


    }
}

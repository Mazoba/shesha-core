using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.Domain
{
    /// <summary>
    /// A Front-end App represents the various front-end applications that build off this back-end. 
    /// For example, Admin Portal, Customer Portal, Customer Mobile App would be fairly typical examples.
    /// </summary>
    public class FrontEndApp : FullPowerEntity
    {
        /// <summary>
        /// Name of the front-end app.
        /// </summary>
        [StringLength(100)]
        public virtual string Name { get; set; }

        /// <summary>
        /// Description of the Front-end application.
        /// </summary>
        public virtual string Description { get; set; }

    }
}

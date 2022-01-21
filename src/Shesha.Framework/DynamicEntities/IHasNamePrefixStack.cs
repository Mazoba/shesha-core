using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.DynamicEntities
{
    /// <summary>
    /// Declares common interface of service that has a prefix stack. Is used for recursive operations
    /// </summary>
    public interface IHasNamePrefixStack
    {
        /// <summary>
        /// Opens new prefix context
        /// </summary>
        /// <param name="prefix"></param>
        IDisposable OpenNamePrefix(string prefix);

        /// <summary>
        /// Current prefix value
        /// </summary>
        string CurrentPrefix { get; }
    }
}

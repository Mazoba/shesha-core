using Shesha.Authorization.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha
{
    /// <summary>
    /// Interface for a class used to determine which page a user should be directed
    /// to by default straight after successfully loging in.
    /// </summary>
    public interface IHomePageRouter
    {

        Task<string> GetHomePageUrlAsync(User user);

    }
}

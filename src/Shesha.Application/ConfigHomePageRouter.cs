using Microsoft.Extensions.Configuration;
using Shesha.Authorization.Users;
using Shesha.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha
{
    /// <summary>
    /// Specifies the page a user should be directed
    /// to by default straight after successfully loging in based on the AppSettings
    /// </summary>
    public class ConfigHomePageRouter : IHomePageRouter
    {
        //private readonly IConfiguration _config;

        //public ConfigHomePageRouter()
        //{
        //    _config = config;
        //}

        public async Task<string> GetHomePageUrlAsync(User user)
        {
            var config = StaticContext.IocManager.Resolve<IConfiguration>();

            var homeUrl = config["SheshaApp:HomeUrl"];

            return homeUrl ?? string.Empty;
        }

    }
}

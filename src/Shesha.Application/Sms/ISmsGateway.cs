﻿using System.Threading.Tasks;

namespace Shesha.Sms
{
    /// <summary>
    /// SMS gateway
    /// </summary>
    public interface ISmsGateway
    {
        /// <summary>
        /// Send sms to specified mobile number
        /// </summary>
        Task SendSmsAsync(string mobileNumber, string body);
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Abp.Configuration;
using Castle.Core.Logging;
using Shesha.Attributes;
using Shesha.Utilities;

namespace Shesha.Sms.Clickatell
{
    [ClassUid("fb8e8757-d831-41a3-925f-fd5c5088ef9b")]
    [Display(Name = "Clickatell")]
    public class ClickatellSmsGateway : ISmsGateway
    {
        // https://archive.clickatell.com/developers/api-docs/http-sending-messages/

        public ILogger Logger { get; set; }
        private readonly ISettingManager _settingManager;

        public ClickatellSmsGateway(ISettingManager settingManager)
        {
            Logger = NullLogger.Instance;
            _settingManager = settingManager;
        }

        /// <summary>
        /// Sends an SMS message.
        /// </summary>
        /// <param name="mobileNumber">Mobile number to send message to. Must be a South African number.</param>
        /// <param name="body">Message to be sent.</param>
        /// <returns>Returns true if message was accepted by the gateway, else returns false.</returns>
        public async Task SendSmsAsync(string mobileNumber, string body)
        {
            if (string.IsNullOrEmpty(mobileNumber))
                throw new Exception("Can't send message, mobile number is empty");

            if (string.IsNullOrEmpty(body))
                throw new Exception("Can't send empty message");

            // Removing any spaces and any other common characters in a phone number.
            mobileNumber = MobileHelper.CleanupmobileNo(mobileNumber);

            var clickatellHost = await _settingManager.GetSettingValueForApplicationAsync(ClickatellSettingNames.Host);
            var clickatellUsername = await _settingManager.GetSettingValueForApplicationAsync(ClickatellSettingNames.ApiUsername);
            var clickatellPassword = await _settingManager.GetSettingValueForApplicationAsync(ClickatellSettingNames.ApiPassword);
            var clickatellApiId = await _settingManager.GetSettingValueForApplicationAsync(ClickatellSettingNames.ApiId);

            var singleMessageMaxLength = (await _settingManager.GetSettingValueForApplicationAsync(ClickatellSettingNames.SingleMessageMaxLength)).ToInt(ClickatellSettingProvider.DefaultSingleMessageMaxLength);
            var messagePartLength = (await _settingManager.GetSettingValueForApplicationAsync(ClickatellSettingNames.MessagePartLength)).ToInt(ClickatellSettingProvider.DefaultMessagePartLength);

            var sb = new StringBuilder();
            sb.Append("https://" + clickatellHost + "/http/sendmsg?api_id=");
            sb.Append(clickatellApiId);
            sb.Append("&user=");
            sb.Append(clickatellUsername);
            sb.Append("&password=");
            sb.Append(clickatellPassword);
            sb.Append("&to=");
            sb.Append(mobileNumber);
            sb.Append("&text=");
            sb.Append(HttpUtility.UrlEncode(body));

            if (body.Length > singleMessageMaxLength)
            {
                var splitCount = body.Length / messagePartLength;
                if (splitCount * messagePartLength < body.Length)
                    splitCount++;

                sb.Append("&concat=" + splitCount);
            }

            Logger.InfoFormat("Sending SMS to {0}: {1}", mobileNumber, body);

            string response = await DownloadUrlAsync(sb.ToString());

            // If response format is 'ID: XXXXXXXXXXXXXXXX' where XXXXXXXXXXXXXX is a message id then request has been successful.
            if (!response.StartsWith("ID:"))
            {
                var exceptionMessage = $"Could not send SMS to {mobileNumber}. Response: {response}";
                Logger.ErrorFormat(exceptionMessage);

                throw new Exception("Could not send SMS to " + mobileNumber + ". Please contact system administrator", new Exception(response));
            }

            Logger.InfoFormat("SMS successfully sent, response: {0}", response);
        }

        public async Task<string> DownloadUrlAsync(string url)
        {
            var request = WebRequest.Create(url);

            var useProxy = await _settingManager.GetSettingValueForApplicationAsync(ClickatellSettingNames.UseProxy) == true.ToString();

            if (useProxy)
            {
                var proxyAddress = await _settingManager.GetSettingValueForApplicationAsync(ClickatellSettingNames.WebProxyAddress);

                var proxy = new WebProxy
                {
                    Address = new Uri(proxyAddress)
                };
                request.Proxy = proxy;

                var useDefaultCredentials = await _settingManager.GetSettingValueForApplicationAsync(ClickatellSettingNames.UseDefaultProxyCredentials) == true.ToString();
                if (useDefaultCredentials)
                {
                    proxy.Credentials = CredentialCache.DefaultCredentials;
                    proxy.UseDefaultCredentials = true;
                }
                else
                {
                    var username = await _settingManager.GetSettingValueForApplicationAsync(ClickatellSettingNames.WebProxyUsername);
                    var password = await _settingManager.GetSettingValueForApplicationAsync(ClickatellSettingNames.WebProxyPassword);

                    proxy.Credentials = new NetworkCredential(username, password);
                }
            }

            using (var response = await request.GetResponseAsync())
            {
                await using (var receiveStream = response.GetResponseStream())
                {
                    if (receiveStream == null)
                        return null;

                    var readStream = new StreamReader(receiveStream, Encoding.GetEncoding("utf-8"));
                    var strResponse = await readStream.ReadToEndAsync();

                    return strResponse;
                }
            }
        }
    }

}
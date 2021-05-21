using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Abp.Configuration;
using Castle.Core.Logging;
using Shesha.Attributes;

namespace Shesha.Sms.SmsPortal
{
    [ClassUid("2a85c238-9648-4292-8849-44c61f5ab705")]
    [Display(Name = "Sms Portal")]
    public class SmsPortalGateway : ISmsGateway
    {
        public ILogger Logger { get; set; }
        private readonly ISettingManager _settingManager;

        public SmsPortalGateway(ISettingManager settingManager)
        {
            _settingManager = settingManager;
            Logger = NullLogger.Instance;
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

            /*
            if (body.Length > 160)
            {
                errorMessage = "Message length must be 160 characters or less";
                return false;
            }
            */
            // Removing any spaces and any other common characters in a phone number.
            mobileNumber = mobileNumber.Replace(" ", "");
            mobileNumber = mobileNumber.Replace("-", "");
            mobileNumber = mobileNumber.Replace("(", "");
            mobileNumber = mobileNumber.Replace(")", "");

            // todo: move to a separate class
            // Converting to the required format i.e. '27XXXXXXXXX'
            if (mobileNumber.StartsWith("0027"))
                mobileNumber = "27" + mobileNumber.Substring(4);

            if (mobileNumber.StartsWith("0"))
                mobileNumber = "27" + mobileNumber.Substring(1);

            var smsPortalHost = await _settingManager.GetSettingValueForApplicationAsync(SmsPortalSettingNames.Host);
            var smsPortalUsername = await _settingManager.GetSettingValueForApplicationAsync(SmsPortalSettingNames.Username);
            var smsPortalPassword = await _settingManager.GetSettingValueForApplicationAsync(SmsPortalSettingNames.Password);
            
            var sb = new StringBuilder();
            sb.Append("http://" + smsPortalHost + "?Type=sendparam&Username=");
            sb.Append(smsPortalUsername);
            sb.Append("&password=");
            sb.Append(smsPortalPassword);
            sb.Append("&numto=");
            sb.Append(mobileNumber);
            sb.Append("&data1=");
            sb.Append(HttpUtility.UrlEncode(body));

            Logger.InfoFormat("Sending SMS to {0}: {1}", mobileNumber, body);

            string response = await DownloadUrlAsync(sb.ToString());

            var xml = new XmlDocument();
            xml.LoadXml(response); // suppose that myXmlString contains "<Names>...</Names>"

            var node = xml.SelectSingleNode("/api_result/call_result");
            var result = node["result"].InnerText;

            // If response format is <api_result><send_info><eventid>XXXXXX</eventid></send_info>
            //<call_result><result>True</result><error /></call_result></api_result> where XXXXXXXXXXXXXX is a /event id then request has been successful.
            if (!result.Equals("True"))
            {
                var error = node["error"].InnerText;
                
                // log with response
                var exceptionMessage = $"Could not send SMS to {mobileNumber}. Response: {error}";
                Logger.ErrorFormat(exceptionMessage);

                throw new Exception("Could not send SMS to " + mobileNumber + ". Please contact system administrator");
            }

            Logger.InfoFormat("SMS successfully sent, response: {0}", response);
        }

        public async Task<string> DownloadUrlAsync(string url)
        {
            var request = WebRequest.Create(url);
            var useProxy = await _settingManager.GetSettingValueForApplicationAsync(SmsPortalSettingNames.UseProxy) == true.ToString();

            if (useProxy)
            {
                var proxyAddress = await _settingManager.GetSettingValueForApplicationAsync(SmsPortalSettingNames.WebProxyAddress);

                var proxy = new WebProxy
                {
                    Address = new Uri(proxyAddress)
                };
                request.Proxy = proxy;

                var useDefaultCredentials = await _settingManager.GetSettingValueForApplicationAsync(SmsPortalSettingNames.UseDefaultProxyCredentials) == true.ToString();
                if (useDefaultCredentials)
                {
                    proxy.Credentials = CredentialCache.DefaultCredentials;
                    proxy.UseDefaultCredentials = true;
                }
                else
                {
                    var username = await _settingManager.GetSettingValueForApplicationAsync(SmsPortalSettingNames.WebProxyUsername);
                    var password = await _settingManager.GetSettingValueForApplicationAsync(SmsPortalSettingNames.WebProxyPassword);

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
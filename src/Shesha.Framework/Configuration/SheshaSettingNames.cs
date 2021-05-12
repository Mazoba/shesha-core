namespace Shesha.Configuration
{
    public static class SheshaSettingNames
    {
        public const string UploadFolder = "Shesha.UploadFolder";

        public const string ExchangeName = "Shesha.ExchangeName";

        public static class Security
        {
            public const string AutoLogoffTimeout = "Shesha.Security.AutoLogoffTimeout";
        }

        public static class Sms
        {
            public const string SmsGateway = "Shesha.Sms.SmsGateway";
            public const string RedirectAllMessagesTo = "Shesha.Sms.RedirectAllMessagesTo";
        }

        public static class Push
        {
            public const string PushNotifier = "Shesha.Push.PushNotifier";
            public const string PushNotificationsEnabled = "Shesha.Push.PushNotificationsEnabled";
        }

        public static class Email
        {
            public const string SupportSmtpRelay = "Shesha.Email.SupportSmtpRelay";
            public const string RedirectAllMessagesTo = "Shesha.Email.RedirectAllMessagesTo";
            public const string EmailsEnabled = "Shesha.Email.EmailsEnabled";
        }
    }
}

namespace Shesha.Authorization.Settings.Dto
{
    /// <summary>
    /// Authorization options
    /// </summary>
    public class AuthorizationSettingsDto
    {
        /// <summary>
        /// Lockout enabled (default value for new users)
        /// </summary>
        public bool IsLockoutEnabled { get; set; }
        
        /// <summary>
        /// Lockout time in seconds
        /// </summary>
        public int DefaultAccountLockoutSeconds { get; set; }

        /// <summary>
        /// Max failed logon attempts before lockout
        /// </summary>
        public int MaxFailedAccessAttemptsBeforeLockout { get; set; }


        /// <summary>
        /// Passwords: require digits
        /// </summary>
        public bool RequireDigit { get; set; }

        /// <summary>
        /// Passwords: require lower case character
        /// </summary>
        public bool RequireLowercase { get; set; }

        /// <summary>
        /// Passwords: non alphanumeric character
        /// </summary>
        public bool RequireNonAlphanumeric { get; set; }
        /// <summary>
        /// Passwords: require upper case character
        /// </summary>
        public bool RequireUppercase { get; set; }
        
        /// <summary>
        /// Passwords: min length
        /// </summary>
        public int RequiredLength { get; set; }

        /// <summary>
        /// Auto logoff timeout (in case of user inactivity). Set to 0 to disable
        /// </summary>
        public int AutoLogoffTimeout { get; set; }
    }
}

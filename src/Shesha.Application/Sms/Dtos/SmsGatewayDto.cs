namespace Shesha.Sms.Dtos
{
    /// <summary>
    /// SMS Gateway DTO
    /// </summary>
    public class SmsGatewayDto
    {
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Unique identifier of the Sms Gateway
        /// </summary>
        public string Uid { get; set; }
    }
}
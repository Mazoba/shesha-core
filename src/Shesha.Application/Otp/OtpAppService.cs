﻿using System;
using System.Threading.Tasks;
using Abp.Configuration;
using Abp.Dependency;
using Abp.Net.Mail;
using Abp.UI;
using Microsoft.AspNetCore.Mvc;
using Shesha.Domain.Enums;
using Shesha.Exceptions;
using Shesha.Otp.Configuration;
using Shesha.Otp.Dto;
using Shesha.Services;
using Shesha.Sms;
using Shesha.Utilities;

namespace Shesha.Otp
{
    public class OtpAppService : SheshaAppServiceBase, IOtpAppService, ITransientDependency
    {
        private readonly ISmsGateway _smsGateway;
        private readonly IEmailSender _emailSender;
        private readonly IOtpStorage _otpStorage;
        private readonly IOtpGenerator _otpGenerator;
        private readonly ISettingManager _settingManager;
        private readonly IOtpSettings _otpSettings;

        public OtpAppService(ISmsGateway smsGateway, IEmailSender emailSender, IOtpStorage otpStorage, IOtpGenerator passwordGenerator, ISettingManager settingManager, IOtpSettings otpSettings)
        {
            _smsGateway = smsGateway;
            _emailSender = emailSender;
            _otpStorage = otpStorage;
            _otpGenerator = passwordGenerator;
            _settingManager = settingManager;
            _otpSettings = otpSettings;
        }

        /// <summary>
        /// Send one-time-pin
        /// </summary>
        public async Task<SendPinDto> SendPinAsync(SendPinInput input)
        {
            if (string.IsNullOrWhiteSpace(input.SendTo))
                throw new Exception($"{input.SendTo} must be specified");

            // generate new pin and save
            var otp = new OtpDto()
            {
                OperationId = Guid.NewGuid(),
                Pin = _otpGenerator.GeneratePin(),

                SendTo = input.SendTo,
                SendType = input.SendType,
                RecipientId = input.RecipientId,
                RecipientType = input.RecipientType,
                ActionType = input.ActionType,
            };

            // send otp
            if (_otpSettings.IgnoreOtpValidation)
            {
                otp.SendStatus = OtpSendStatus.Ignored;
            } else
            {
                try
                {
                    otp.SentOn = DateTime.Now;

                    await SendInternal(otp);
                    
                    otp.SendStatus = OtpSendStatus.Sent;
                }
                catch (Exception e)
                {
                    otp.SendStatus = OtpSendStatus.Failed;
                    otp.ErrorMessage = e.FullMessage();
                }
            }

            // set expiration and save
            var lifeTime = input.Lifetime ?? _otpSettings.DefaultLifetime;
            if (lifeTime == 0)
                lifeTime = OtpSettingProvider.DefaultLifetime;

            otp.ExpiresOn = DateTime.Now.AddSeconds(lifeTime);

            await _otpStorage.SaveAsync(otp);
            
            // return response
            var response = new SendPinDto
            {
                OperationId = otp.OperationId,
                SentTo = otp.SendTo
            };
            return response;
        }

        /// <summary>
        /// Resend one-time-pin
        /// </summary>
        public async Task<SendPinDto> ResendPinAsync(ResendPinInput input)
        {
            var otp = await _otpStorage.GetAsync(input.OperationId);
            if (otp == null)
                throw new UserFriendlyException("OTP not found, try to request a new one");

            if (otp.ExpiresOn < DateTime.Now)
                throw new UserFriendlyException("OTP has expired, try to request a new one");

            // note: we ignore _otpSettings.IgnoreOtpValidation here, the user pressed `resend` manually

            // send otp
            var sendTime = DateTime.Now;
            try
            {
                await SendInternal(otp);
            }
            catch (Exception e)
            {
                await _otpStorage.UpdateAsync(input.OperationId, newOtp =>
                {
                    newOtp.SentOn = sendTime;
                    newOtp.SendStatus = OtpSendStatus.Failed;
                    newOtp.ErrorMessage = e.FullMessage();
                    
                    return Task.CompletedTask;
                });
            }
            
            // extend lifetime
            var lifeTime = input.Lifetime ?? _otpSettings.DefaultLifetime;
            var newExpiresOn = DateTime.Now.AddSeconds(lifeTime);

            await _otpStorage.UpdateAsync(input.OperationId, newOtp =>
            {
                newOtp.SentOn = sendTime;
                newOtp.SendStatus = OtpSendStatus.Sent;
                newOtp.ExpiresOn = newExpiresOn;

                return Task.CompletedTask;
            });

            // return response
            var response = new SendPinDto
            {
                OperationId = otp.OperationId,
                SentTo = otp.SendTo
            };
            return response;
        }

        private async Task SendInternal(OtpDto otp)
        {
            switch (otp.SendType)
            {
                case OtpSendType.Sms:
                {
                    var bodyTemplate = await _settingManager.GetSettingValueAsync(OtpSettingsNames.DefaultBodyTemplate);
                    if (string.IsNullOrWhiteSpace(bodyTemplate))
                        bodyTemplate = OtpSettingProvider.DefaultBodyTemplate;

                    // todo: use mustache
                    var messageBody = bodyTemplate.Replace("{{password}}", otp.Pin);
                    await _smsGateway.SendSmsAsync(otp.SendTo, messageBody);
                    break;
                }
                case OtpSendType.Email:
                {
                    var bodyTemplate = await _settingManager.GetSettingValueAsync(OtpSettingsNames.DefaultBodyTemplate);
                    var subjectTemplate = await _settingManager.GetSettingValueAsync(OtpSettingsNames.DefaultSubjectTemplate);

                    var body = bodyTemplate.Replace("{{password}}", otp.Pin);
                    var subject= subjectTemplate.Replace("{{password}}", otp.Pin);

                    await _emailSender.SendAsync(otp.SendTo, subject, body, false);
                    break;
                }
                default:
                    throw new NotSupportedException($"unsupported {nameof(otp.SendType)}: {otp.SendType}");
            }
        }

        /// <summary>
        /// Verify one-time-pin
        /// </summary>
        public async Task<VerifyPinResponse> VerifyPinAsync(VerifyPinInput input)
        {
            if (!_otpSettings.IgnoreOtpValidation)
            {
                var pinDto = await _otpStorage.GetAsync(input.OperationId);
                if (pinDto == null || pinDto.Pin != input.Pin)
                    return VerifyPinResponse.Failed("Wrong one time pin");

                if (pinDto.ExpiresOn < DateTime.Now)
                    return VerifyPinResponse.Failed("One-time pin has expired, try to send a new one");
            }

            return VerifyPinResponse.Success();
        }

        /// inheritedDoc
        public async Task<OtpDto> GetAsync(Guid operationId)
        {
            return await _otpStorage.GetAsync(operationId);
        }

        /// inheritDoc
        [HttpPost]
        public async Task<bool> UpdateSettingsAsync(OtpSettingsDto input)
        {
            await _settingManager.ChangeSettingForApplicationAsync(OtpSettingsNames.PasswordLength, input.PasswordLength.ToString());
            await _settingManager.ChangeSettingForApplicationAsync(OtpSettingsNames.Alphabet, input.Alphabet);
            await _settingManager.ChangeSettingForApplicationAsync(OtpSettingsNames.DefaultLifetime, input.DefaultLifetime.ToString());
            await _settingManager.ChangeSettingForApplicationAsync(OtpSettingsNames.IgnoreOtpValidation, input.IgnoreOtpValidation.ToString());

            return true;
        }

        /// inheritDoc
        [HttpGet]
        public async Task<OtpSettingsDto> GetSettingsAsync()
        {
            var settings = new OtpSettingsDto
            {
                PasswordLength = (await _settingManager.GetSettingValueForApplicationAsync(OtpSettingsNames.PasswordLength)).ToInt(OtpSettingProvider.DefaultPasswordLength),
                Alphabet = await _settingManager.GetSettingValueForApplicationAsync(OtpSettingsNames.Alphabet),
                DefaultLifetime = (await _settingManager.GetSettingValueForApplicationAsync(OtpSettingsNames.DefaultLifetime)).ToInt(OtpSettingProvider.DefaultLifetime),
                IgnoreOtpValidation = await _settingManager.GetSettingValueForApplicationAsync(OtpSettingsNames.IgnoreOtpValidation) == true.ToString()
            };
            
            return settings;
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Castle.MicroKernel.Registration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using NHibernate.Linq;
using Shesha.Authentication.JwtBearer;
using Shesha.Authorization;
using Shesha.Authorization.Users;
using Shesha.Models.TokenAuth;
using Shesha.Otp;
using Shesha.Otp.Dto;
using Shesha.Users;
using Shesha.Users.Dto;
using Shouldly;
using Xunit;

namespace Shesha.Tests.Users
{
    public class DomainModel_Tests : SheshaNhTestBase
    {
        private readonly IRepository<User, Int64> _userRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public DomainModel_Tests()
        {
            _userRepository = Resolve<IRepository<User, Int64>>();
            _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
        }

        [Fact]
        public async Task ResetPasswordUsingOtp_Test()
        {
            var oldPassword = "123qwe";
            var mobileNo = await GetUniqueMobileNo(); // todo: mock user store to prevent duplicated mobile numbers and remove this code
            var newPassword = "!!!Pass1234";
            var userName = Guid.NewGuid().ToString("N");

            var currentPin = string.Empty;

            LoginAsHostAdmin();

            // Configure Token Auth (required by the TokenAuthController)
            ConfigureTokenAuth();

            #region OTP hack 

            var storage = new Dictionary<Guid, string>();
            var otpStorage = new Mock<IOtpStorage>();
            otpStorage.Setup(s => s.SaveAsync(It.IsAny<OtpDto>())).Returns<OtpDto>(dto =>
            {
                currentPin = dto.Pin;
                storage.Add(dto.OperationId, dto.Pin);
                return Task.CompletedTask;
            });
            otpStorage.Setup(s => s.GetAsync(It.IsAny<Guid>())).Returns<Guid>(id => Task.FromResult(new OtpDto
            {
                Pin = storage[id],
                OperationId = id,
                ExpiresOn = DateTime.MaxValue
            }));

            LocalIocManager.IocContainer.Register(Component.For<IOtpStorage>().Instance(otpStorage.Object).IsDefault());

            #endregion

            // get IUserAppService with hacked otp storage
            var userAppService = LocalIocManager.Resolve<IUserAppService>();


            // create new user and set mobile number
            var userDto = await userAppService.CreateAsync(
                new CreateUserDto
                {
                    EmailAddress = $"{userName}@domain.com",
                    IsActive = true,
                    Name = "Test",
                    Surname = "User",
                    Password = oldPassword,
                    UserName = userName
                });

            // try to login using current password
            var firstLoginAttempt = await ValidateCredentials(userName, oldPassword);
            firstLoginAttempt.ShouldBeTrue("Failed to login as a new user");

            using (var uow = _unitOfWorkManager.Begin())
            {
                var user = await _userRepository.GetAll().FirstOrDefaultAsync(u => u.UserName == userName);
                user.PhoneNumber = mobileNo;
                await _userRepository.UpdateAsync(user);
                
                await uow.CompleteAsync();
            }
            
            // send OTP for password reset
            var sendOtpResponse = await userAppService.ResetPasswordSendOtp(mobileNo);

            // verify response
            var verifyResponse = await userAppService.ResetPasswordVerifyOtp(new ResetPasswordVerifyOtpInput()
            {
                MobileNo = mobileNo,
                OperationId = sendOtpResponse.OperationId,
                Pin = currentPin
            });

            // change password
            var resetPasswordResponse =
                await userAppService.ResetPasswordUsingToken(new ResetPasswordUsingTokenInput()
                {
                    Username = verifyResponse.Username,
                    Token = verifyResponse.Token,
                    NewPassword = newPassword
                });

            // try to login using old password
            var failedAttempt = await ValidateCredentials(userName, oldPassword);
            
            // try to login using new password
            var successAttempt = await ValidateCredentials(userName, newPassword);
            
            // remove the user
            await _userRepository.DeleteAsync(userDto.Id);

            // check only after deletion
            resetPasswordResponse.ShouldBeTrue("Failed to set new password using correct token");
            failedAttempt.ShouldBeFalse("Old password still work");
            successAttempt.ShouldBeTrue("New password doesn't work");
        }

        private async Task<string> GetUniqueMobileNo()
        {
            var rnd = new Random();
            using (var uow = _unitOfWorkManager.Begin())
            {
                do
                {
                    var mobileNo = rnd.NextDouble().ToString("0000000000");
                    var alreadyExists = await _userRepository.GetAll().FirstOrDefaultAsync(u => u.PhoneNumber == mobileNo) != null;
                    if (!alreadyExists)
                        return mobileNo;
                } while (true);
            }
        }

        private async Task<bool> ValidateCredentials(string username, string password)
        {
            try
            {
                using (var uow = _unitOfWorkManager.Begin())
                {
                    var controller = Resolve<TokenAuthController>();
                    var response = await controller.Authenticate(new AuthenticateModel()
                    {
                        UserNameOrEmailAddress = username,
                        Password = password
                    });
                    await uow.CompleteAsync();

                    return !string.IsNullOrWhiteSpace(response.AccessToken);
                }
            }
            catch
            {
                return false;
            }
        }

        private void ConfigureTokenAuth()
        {
            LocalIocManager.Register<TokenAuthConfiguration>();
            var tokenAuthConfig = LocalIocManager.Resolve<TokenAuthConfiguration>();
            
            tokenAuthConfig.SecurityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("SheshaTest_C421AAEE0D114E9C"));
            tokenAuthConfig.Issuer = "SheshaTest";
            tokenAuthConfig.Audience = "SheshaTest";
            tokenAuthConfig.SigningCredentials = new SigningCredentials(tokenAuthConfig.SecurityKey, SecurityAlgorithms.HmacSha256);
            tokenAuthConfig.Expiration = TimeSpan.FromDays(1);
        }

        [Fact]
        public async Task CreateUser_Test()
        {
            LoginAsHostAdmin();

            var userAppService = LocalIocManager.Resolve<IUserAppService>();

            var userName = Guid.NewGuid().ToString("N");

            await userAppService.CreateAsync(
                new CreateUserDto
                {
                    EmailAddress = $"{userName}@domain.com",
                    IsActive = true,
                    Name = "Test",
                    Surname = "User",
                    Password = "123qwe",
                    UserName = userName
                });

            await UsingDbSessionAsync(async session =>
            {
                var johnNashUser = await session.Query<User>().FirstOrDefaultAsync(u => u.UserName == userName);
                johnNashUser.ShouldNotBeNull();
            });
        }
    }
}

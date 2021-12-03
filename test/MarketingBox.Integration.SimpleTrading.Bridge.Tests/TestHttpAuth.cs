﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MarketingBox.Integration.Service.Grpc.Models.Common;
using MarketingBox.Integration.Service.Grpc.Models.Registrations.Contracts.Bridge;
using MarketingBox.Integration.SimpleTrading.Bridge.Services;
using MarketingBox.Integration.SimpleTrading.Bridge.Services.Integrations;
using MarketingBox.Integration.SimpleTrading.Bridge.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace MarketingBox.Integration.SimpleTrading.Bridge.Tests
{
    public class TestHttpAuth
    {
        private Activity _unitTestActivity;
        private SettingsModel _settingsModel;
        private SimpleTradingHttpClient _httpClient;
        private static Random random = new Random();
        private ILogger<BridgeService> _logger;
        private BridgeService _registerService;

        public void Dispose()
        {
            _unitTestActivity.Stop();
        }

        public static string RandomDigitString(int length)
        {
            const string chars = "123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }


        [SetUp]
        public void Setup()
        {
            _settingsModel = new SettingsModel()
            {
                SeqServiceUrl = "http://192.168.1.80:5341",
                BrandAffiliateId = "1027",
                //BrandAffiliateKey = "c23b69afad61464191d067bb166d9511",
                BrandAffiliateKey = "",
                BrandBrandId = "HandelPro-ST",
                BrandUrl = "https://integration-test.mnftx.biz/",
            };

            _unitTestActivity = new Activity("UnitTest").Start();
            _httpClient = new SimpleTradingHttpClient(_settingsModel.BrandUrl);
            _logger = Mock.Of<ILogger<BridgeService>>();
            _registerService = new BridgeService(_logger, _httpClient, _settingsModel);
        }


        [Test]
        public async Task ServiceRequaredAuthHttpSend()
        {
            var dt = DateTime.UtcNow;
            var bridgeRequest = new RegistrationRequest()
            {
                Info = new Service.Grpc.Models.Registrations.RegistrationInfo()
                {
                    FirstName = "Yuriy",
                    LastName = "Test",
                    Phone = "+79995556677",
                    Email = "yuriy.test." + dt.ToString("yyyy.MM.dd") + "." + RandomDigitString(3) + "@mailinator.com",
                    Password = "Trader123",
                    Ip = "99.99.99.99",
                    Country = "PL",
                    Language = "EN",
                }
            };
            var result = await _registerService.SendRegistrationAsync(bridgeRequest);

            Assert.AreEqual(ResultCode.Failed, result.ResultCode);
        }
    }
}

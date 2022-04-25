using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MarketingBox.Bridge.SimpleTrading.Service.Services;
using MarketingBox.Bridge.SimpleTrading.Service.Services.Integrations;
using MarketingBox.Bridge.SimpleTrading.Service.Services.Integrations.Contracts.Requests;
using MarketingBox.Bridge.SimpleTrading.Service.Settings;
using MarketingBox.Integration.Service.Grpc.Models.Common;
using MarketingBox.Sdk.Common.Models.Grpc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace MarketingBox.Bridge.SimpleTrading.Service.Tests
{
    public class TestHttpCreateRegistration
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
                BrandAffiliateKey = "c23b69afad61464191d067bb166d9511",
                BrandBrandId = "HandelPro-ST",
                BrandUrl = "https://integration-test.mnftx.biz/",
            };

            _unitTestActivity = new Activity("UnitTest").Start();
            _httpClient = new SimpleTradingHttpClient(_settingsModel.BrandUrl);
            _logger = Mock.Of<ILogger<BridgeService>>();
            _registerService = new BridgeService(_logger, _httpClient, _settingsModel);
        }

        [Test]
        public async Task DirectHttpSend()
        {
            var dt = DateTime.UtcNow;
            var request = new RegistrationRequest()
            {
                AffId = Convert.ToInt32(_settingsModel.BrandAffiliateId),
                BrandId = _settingsModel.BrandBrandId,
                SecretKey = _settingsModel.BrandAffiliateKey,
                //-----
                FirstName = "Yuriy",
                LastName = "Test",
                Phone = "+79995556677",
                Email = "yuriy.test." + dt.ToString("yyyy.MM.dd") + "." + RandomDigitString(3) + "@mailinator.com",
                Password = "Trader123",
                Ip = "99.99.99.99",
                CountryOfRegistration = "PL",
                CountryByIp = "PL",
                LangId = "EN",
                UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 12_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/12.0 Mobile/15E148 Safari/604.1",
                CxdToken = "handelpro2_11111_111111",
                ProcessId = dt.ToString("u"),
                LandingPage = @"https://landingPage.online/nb_1st_pfizer_hp_st_pl/?sub_id=101211&offer_id=28",
                RedirectedFromUrl = @"https://redirectedFromUrl.online/nb_1st_pfizer_hp_st_pl/?sub_id=101211&offer_id=28"
            };

            var result = await _httpClient.RegisterTraderAsync(request);
            Assert.AreEqual(true, !result.IsFailed);
        }

        [Test]
        public async Task ServiceHttpSend()
        {
#if !DEBUG
            Assert.Pass();
#endif
            var dt = DateTime.UtcNow;
            var request = new RegistrationRequest()
            {
                AffId = Convert.ToInt32(_settingsModel.BrandAffiliateId),
                BrandId = _settingsModel.BrandBrandId,
                SecretKey = _settingsModel.BrandAffiliateKey,
                //-----
                FirstName = "Yuriy",
                LastName = "Test",
                Phone = "+79995556677",
                Email = "yuriy.test." + dt.ToString("yyyy.MM.dd") + "." + RandomDigitString(3) + "@mailinator.com",
                Password = "Trader123",
                Ip = "99.99.99.99",
                CountryOfRegistration = "PL",
                CountryByIp = "PL",
                LangId = "EN",
                UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 12_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/12.0 Mobile/15E148 Safari/604.1",
                CxdToken = "handelpro2_11111_111111",
                ProcessId = dt.ToString("u"),
                LandingPage = @"https://landingPage.online/nb_1st_pfizer_hp_st_pl/?sub_id=101211&offer_id=28",
                RedirectedFromUrl = @"https://redirectedFromUrl.online/nb_1st_pfizer_hp_st_pl/?sub_id=101211&offer_id=28"
            };

            var result = await _registerService.RegisterExternalCustomerAsync(request);
            Assert.AreEqual(ResponseStatus.Ok, result.Status);
        }


        [Test]
        public async Task ServiceAlreadyExistHttpSend()
        {
            var dt1 = DateTimeOffset.Now;
            var dt2 = dt1.AddMilliseconds(100);
            var email = "yuriy.test." + dt1.ToString("yyyy.MM.dd") + "." + RandomDigitString(3) + "@mailinator.com";
            var processId1 = dt1.ToString("yyyy-MM-ddThh:mm:ss.fffZ") + " " + email;
            var processId2 = dt2.ToString("yyyy-MM-ddThh:mm:ss.fffZ") + " " + email;

            var request1 = new RegistrationRequest()
            {
                AffId = Convert.ToInt32(_settingsModel.BrandAffiliateId),
                BrandId = _settingsModel.BrandBrandId,
                SecretKey = _settingsModel.BrandAffiliateKey,
                //-----
                FirstName = "Yuriy",
                LastName = "Test",
                Phone = "+79995556677",
                Email = email,
                Password = "Trader123",
                Ip = "99.99.99.99",
                CountryOfRegistration = "PL",
                CountryByIp = "PL",
                LangId = "EN",
                UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 12_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/12.0 Mobile/15E148 Safari/604.1",
                CxdToken = "handelpro2_11111_111111",
                ProcessId = processId1,
                LandingPage = @"https://landingPage.online/nb_1st_pfizer_hp_st_pl/?sub_id=101211&offer_id=28",
                RedirectedFromUrl = @"https://redirectedFromUrl.online/nb_1st_pfizer_hp_st_pl/?sub_id=101211&offer_id=28"
            };

            var request2 = new RegistrationRequest()
            {
                AffId = Convert.ToInt32(_settingsModel.BrandAffiliateId),
                BrandId = _settingsModel.BrandBrandId,
                SecretKey = _settingsModel.BrandAffiliateKey,
                //-----
                FirstName = "Yuriy",
                LastName = "Test",
                Phone = "+79995556677",
                Email = email,
                Password = "Trader123",
                Ip = "99.99.99.99",
                CountryOfRegistration = "PL",
                CountryByIp = "PL",
                LangId = "EN",
                UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 12_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/12.0 Mobile/15E148 Safari/604.1",
                CxdToken = "handelpro2_11111_111111",
                ProcessId = processId2,
                LandingPage = @"https://landingPage.online/nb_1st_pfizer_hp_st_pl/?sub_id=101211&offer_id=28",
                RedirectedFromUrl = @"https://redirectedFromUrl.online/nb_1st_pfizer_hp_st_pl/?sub_id=101211&offer_id=28"
            };
            var result = await _registerService.RegisterExternalCustomerAsync(request1);
            Assert.AreEqual(ResponseStatus.Ok, result.Status);
            // The same registration with another time window
            result = await _registerService.RegisterExternalCustomerAsync(request2);
            Assert.AreEqual(ResponseStatus.InternalError, result.Status);
            Assert.AreEqual(ResponseStatus.BadRequest, result.Status);
        }

        [Test]
        public async Task ServiceDoubleClickHttpSend()
        {
            var dt = DateTimeOffset.Now;
            var email = "yuriy.test." + dt.ToString("yyyy.MM.dd") + "." + RandomDigitString(3) + "@mailinator.com";
            var processId = dt.ToString("yyyy-MM-ddThh:mm:ss.fffZ") + " " + email;


            var request1 = new RegistrationRequest()
            {
                AffId = Convert.ToInt32(_settingsModel.BrandAffiliateId),
                BrandId = _settingsModel.BrandBrandId,
                SecretKey = _settingsModel.BrandAffiliateKey,
                //-----
                FirstName = "Yuriy",
                LastName = "Test",
                Phone = "+79995556677",
                Email = email,
                Password = "Trader123",
                Ip = "99.99.99.99",
                CountryOfRegistration = "PL",
                CountryByIp = "PL",
                LangId = "EN",
                UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 12_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/12.0 Mobile/15E148 Safari/604.1",
                CxdToken = "handelpro2_11111_111111",
                ProcessId = processId,
                LandingPage = @"https://landingPage.online/nb_1st_pfizer_hp_st_pl/?sub_id=101211&offer_id=28",
                RedirectedFromUrl = @"https://redirectedFromUrl.online/nb_1st_pfizer_hp_st_pl/?sub_id=101211&offer_id=28"
            };

            var request2 = new RegistrationRequest()
            {
                AffId = Convert.ToInt32(_settingsModel.BrandAffiliateId),
                BrandId = _settingsModel.BrandBrandId,
                SecretKey = _settingsModel.BrandAffiliateKey,
                //-----
                FirstName = "Yuriy",
                LastName = "Test",
                Phone = "+79995556677",
                Email = email,
                Password = "Trader123",
                Ip = "99.99.99.99",
                CountryOfRegistration = "PL",
                CountryByIp = "PL",
                LangId = "EN",
                UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 12_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/12.0 Mobile/15E148 Safari/604.1",
                CxdToken = "handelpro2_11111_111111",
                ProcessId = processId,
                LandingPage = @"https://landingPage.online/nb_1st_pfizer_hp_st_pl/?sub_id=101211&offer_id=28",
                RedirectedFromUrl = @"https://redirectedFromUrl.online/nb_1st_pfizer_hp_st_pl/?sub_id=101211&offer_id=28"
            };
            var result = await _registerService.RegisterExternalCustomerAsync(request1);
            Assert.AreEqual(ResponseStatus.Ok, result.Status);
            // The same registration with another time window
            result = await _registerService.RegisterExternalCustomerAsync(request2);
            Assert.AreEqual(ResponseStatus.Ok, result.Status);
        }
    }
}

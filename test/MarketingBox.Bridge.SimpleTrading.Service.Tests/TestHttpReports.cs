using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MarketingBox.Bridge.SimpleTrading.Service.Services;
using MarketingBox.Bridge.SimpleTrading.Service.Services.Integrations;
using MarketingBox.Bridge.SimpleTrading.Service.Services.Integrations.Contracts.Requests;
using MarketingBox.Bridge.SimpleTrading.Service.Settings;
using MarketingBox.Integration.Service.Grpc.Models.Registrations.Contracts.Bridge;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace MarketingBox.Bridge.SimpleTrading.Service.Tests
{
    public class TestHttpReports
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
                BrandBrandId = "Monfex-ST",
                BrandUrl = "https://integration-uat.mnftx.biz",
            };

            _unitTestActivity = new Activity("UnitTest").Start();
            _httpClient = new SimpleTradingHttpClient(_settingsModel.BrandUrl);
            _logger = Mock.Of<ILogger<BridgeService>>();
            _registerService = new BridgeService(_logger, _httpClient, _settingsModel);
        }

        [Test]
        public async Task HttpGetRegistrationsAsync()
        {
            var dt = DateTime.Parse("2021-12-01");
            var request = new ReportRequest()
            {
                Year = dt.Year,
                Month = dt.Month - 1,
                Page = 1,
                PageSize = 100,
                ApiKey = _settingsModel.BrandAffiliateKey,
            };

            var result = await _httpClient.GetRegistrationsAsync(request);
            Assert.IsFalse(result.IsFailed);
        }


        [Test]
        public async Task HttpGetDepositsAsync()
        {
            var dt = DateTime.Parse("2021-12-01");
            var request = new ReportRequest()
            {
                Year = dt.Year,
                Month = dt.Month - 1,
                Page = 1,
                PageSize = 100,
                ApiKey = _settingsModel.BrandAffiliateKey,
            };

            var result = await _httpClient.GetDepositsAsync(request);
            Assert.IsFalse(result.IsFailed);
        }


        [Test]
        public async Task HttpGetGetCountsAsync()
        {
            var dt = DateTime.UtcNow;
            var request = new ReportCountersRequest()
            {
                Year = dt.Year,
                Month = dt.Month - 1,
                ApiKey = _settingsModel.BrandAffiliateKey,
            };

            var result = await _httpClient.GetCountsAsync(request);
            Assert.IsFalse(result.IsFailed);
        }

        //[Test]
        public async Task GetRegistrationsPerPeriodAsync()
        {
            var dt = DateTime.Parse("2021-12-01");
            var request = new ReportingRequest()
            {
                DateFrom = dt.AddMonths(-1),
                DateTo = dt,
                PageIndex = 1,
                PageSize = 100,
            };

            var result = await _registerService.GetRegistrationsPerPeriodAsync(request);
            Assert.IsTrue(result.Data.Count > 0);
        }

        [Test]
        public async Task GetRegistrationsPerPeriodEmptyAsync()
        {
            var dt = DateTime.Parse("2020-02-01");
            var request = new ReportingRequest()
            {
                DateFrom = dt.AddMonths(-1),
                DateTo = dt,
                PageIndex = 1,
                PageSize = 100,
            };

            var result = await _registerService.GetRegistrationsPerPeriodAsync(request);
            Assert.IsTrue(result.Data.Count == 0);
        }

        //[Test]
        public async Task GetDepositorsPerPeriodAsync()
        {
            var dt = DateTime.Parse("2021-12-01");
            var request = new ReportingRequest()
            {
                DateFrom = dt.AddMonths(-1),
                DateTo = dt,
                PageIndex = 1,
                PageSize = 100,
            };

            var result = await _registerService.GetDepositorsPerPeriodAsync(request);
            Assert.IsTrue(result.Data.Count > 0);
        }

        [Test]
        public async Task GetDepositorsPerPeriodEmptyAsync()
        {
            var dt = DateTime.Parse("2020-02-01");
            var request = new ReportingRequest()
            {
                DateFrom = dt.AddMonths(-1),
                DateTo = dt,
                PageIndex = 1,
                PageSize = 100,
            };

            var result = await _registerService.GetDepositorsPerPeriodAsync(request);
            Assert.IsTrue(result.Data.Count == 0);
        }


    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarketingBox.Bridge.SimpleTrading.Service.Services.Integrations;
using MarketingBox.Bridge.SimpleTrading.Service.Services.Integrations.Contracts.Enums;
using MarketingBox.Bridge.SimpleTrading.Service.Services.Integrations.Contracts.Requests;
using MarketingBox.Bridge.SimpleTrading.Service.Services.Integrations.Contracts.Responses;
using MarketingBox.Bridge.SimpleTrading.Service.Settings;
using MarketingBox.Integration.Bridge.Client;
using MarketingBox.Integration.Service.Domain.Registrations;
using MarketingBox.Integration.Service.Grpc.Models.Registrations;
using MarketingBox.Sdk.Common.Exceptions;
using MarketingBox.Sdk.Common.Extensions;
using MarketingBox.Sdk.Common.Models.Grpc;
using Microsoft.Extensions.Logging;
using IntegrationBridge = MarketingBox.Integration.Service.Grpc.Models.Registrations.Contracts.Bridge;
using RegistrationRequest =
    MarketingBox.Bridge.SimpleTrading.Service.Services.Integrations.Contracts.Requests.RegistrationRequest;

namespace MarketingBox.Bridge.SimpleTrading.Service.Services
{
    public class BridgeService : IBridgeService
    {
        private readonly ILogger<BridgeService> _logger;
        private readonly ISimpleTradingHttpClient _simpleTradingHttpClient;
        private readonly SettingsModel _settingsModel;

        private static ReportRequest MapToApi(
            IntegrationBridge.ReportingRequest request,
            string authAffApiKey)
        {
            return new ReportRequest()
            {
                Year = request.DateFrom.Year,
                Month = request.DateFrom.Month,
                Page = request.PageIndex,
                PageSize = request.PageSize,
                ApiKey = authAffApiKey
            };
        }

        private static Response<IReadOnlyCollection<RegistrationReporting>> SuccessMapToGrpc(
            ReportRegistrationResponse brandRegistrations)
        {
            var registrations = brandRegistrations.Items.Select(report =>
                new RegistrationReporting
                {
                    Crm = MapCrmStatus(report.CrmStatus),
                    CustomerEmail = report.Email,
                    CustomerId = report.UserId,
                    CreatedAt = report.CreatedAt,
                    CrmUpdatedAt = DateTime.UtcNow
                }).ToList();

            return new Response<IReadOnlyCollection<RegistrationReporting>>
            {
                Status = ResponseStatus.Ok,
                Data = registrations
            };
        }

        private static Response<IReadOnlyCollection<DepositorReporting>> SuccessMapToGrpc(
            ReportDepositResponse brandDeposits)
        {
            var registrations = brandDeposits.Items.Select(report =>
                new DepositorReporting
                {
                    CustomerEmail = report.Email,
                    CustomerId = report.UserId,
                    DepositedAt = report.CreatedAt,
                }).ToList();

            return new Response<IReadOnlyCollection<DepositorReporting>>()
            {
                Status = ResponseStatus.Ok,
                Data = registrations
            };
        }

        private static CrmStatus MapCrmStatus(string status)
        {
            switch (status.ToLower())
            {
                case "new":
                    return CrmStatus.New;

                case "fullyactivated":
                    return CrmStatus.FullyActivated;

                case "highpriority":
                    return CrmStatus.HighPriority;

                case "callback":
                    return CrmStatus.Callback;

                case "failedexpectation":
                    return CrmStatus.FailedExpectation;

                case "notvaliddeletedaccount":
                case "notvalidwrongnumber":
                case "notvalidnophonenumber":
                case "notvalidduplicateuser":
                case "notvalidtestlead":
                case "notvalidunderage":
                case "notvalidnolanguagesupport":
                case "notvalidneverregistered":
                case "notvalidnoneligiblecountries":
                    return CrmStatus.NotValid;

                case "notinterested":
                    return CrmStatus.NotInterested;

                case "transfer":
                    return CrmStatus.Transfer;

                case "followup":
                    return CrmStatus.FollowUp;

                case "noanswer":
                case "autocall":
                    return CrmStatus.NA;

                case "conversionrenew":
                    return CrmStatus.ConversionRenew;

                default:
                    return CrmStatus.Unknown;
            }
        }

        private static RegistrationRequest MapToApi(IntegrationBridge.RegistrationRequest request,
            string authBrandId, int authAffId, string authAffApiKey, string requestId)
        {
            return new RegistrationRequest()
            {
                FirstName = request.Info.FirstName,
                LastName = request.Info.LastName,
                Password = request.Info.Password,
                Email = request.Info.Email,
                Phone = request.Info.Phone,
                LangId = request.Info.Language,
                Ip = request.Info.Ip,
                CountryByIp = request.Info.Country,
                AffId = authAffId,
                BrandId = authBrandId,
                SecretKey = authAffApiKey,
                ProcessId = requestId,
                CountryOfRegistration = request.Info.Country,
            };
        }

        private static Response<CustomerInfo> SuccessMapToGrpc(RegistrationResponse brandRegistrationInfo)
        {
            return new Response<CustomerInfo>()
            {
                Status = ResponseStatus.Ok,
                Data = new CustomerInfo()
                {
                    CustomerId = brandRegistrationInfo.TraderId,
                    LoginUrl = brandRegistrationInfo.RedirectUrl,
                    Token = brandRegistrationInfo.Token
                }
            };
        }


        public BridgeService(ILogger<BridgeService> logger,
            ISimpleTradingHttpClient simpleTradingHttpClient, SettingsModel settingsModel)
        {
            _logger = logger;
            _simpleTradingHttpClient = simpleTradingHttpClient;
            _settingsModel = settingsModel;
        }

        /// <summary>
        /// Register new lead
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<Response<CustomerInfo>> SendRegistrationAsync(
            IntegrationBridge.RegistrationRequest request)
        {
            try
            {
                _logger.LogInformation("Creating new LeadInfo {@context}", request);

                var brandRequest = MapToApi(request, _settingsModel.BrandBrandId,
                    Convert.ToInt32(_settingsModel.BrandAffiliateId), _settingsModel.BrandAffiliateKey,
                    DateTimeOffset.UtcNow.ToString());

                return await RegisterExternalCustomerAsync(brandRequest);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error creating lead {@context}", request);
                return e.FailedResponse<CustomerInfo>();
            }
        }

        /// <summary>
        /// Get all registrations per period
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<Response<IReadOnlyCollection<RegistrationReporting>>> GetRegistrationsPerPeriodAsync(
            IntegrationBridge.ReportingRequest request)
        {
            try
            {
                _logger.LogInformation("GetRegistrationsPerPeriodAsync {@context}", request);

                var brandRequest = MapToApi(request, _settingsModel.BrandAffiliateKey);

                // Get registrations
                var registerResult = await _simpleTradingHttpClient.GetRegistrationsAsync(brandRequest);
                // Failed
                if (registerResult.IsFailed)
                {
                    throw new Exception(registerResult.FailedResult.Message);
                }

                // Success
                return SuccessMapToGrpc(registerResult.SuccessResult);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error get registration reporting {@context}", request);

                return e.FailedResponse<IReadOnlyCollection<RegistrationReporting>>();
            }
        }

        /// <summary>
        /// Get all deposits per period
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<Response<IReadOnlyCollection<DepositorReporting>>> GetDepositorsPerPeriodAsync(
            IntegrationBridge.ReportingRequest request)
        {
            try
            {
                _logger.LogInformation("GetRegistrationsPerPeriodAsync {@context}", request);

                var brandRequest = MapToApi(request, _settingsModel.BrandAffiliateKey);

                // Get deposits
                var depositsResult = await _simpleTradingHttpClient.GetDepositsAsync(brandRequest);
                // Failed
                if (depositsResult.IsFailed)
                {
                    throw new Exception(depositsResult.FailedResult.Message);
                }

                // Success
                return SuccessMapToGrpc(depositsResult.SuccessResult);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error get registration reporting {@context}", request);
                return e.FailedResponse<IReadOnlyCollection<DepositorReporting>>();
            }
        }

        public async Task<Response<CustomerInfo>> RegisterExternalCustomerAsync(
            RegistrationRequest brandRequest)
        {
            var registerResult =
                await _simpleTradingHttpClient.RegisterTraderAsync(brandRequest);

            // Failed
            if (registerResult.IsFailed)
            {
                throw new Exception(registerResult.FailedResult.Message);
            }

            // Success
            if (registerResult.SuccessResult.IsSuccessfully() &&
                (SimpleTradingResultCode) registerResult.SuccessResult.Status ==
                SimpleTradingResultCode.Ok)
            {
                // Success
                return SuccessMapToGrpc(registerResult.SuccessResult);
            }

            throw (SimpleTradingResultCode) registerResult.SuccessResult.Status switch
            {
                // Success, but software failure
                SimpleTradingResultCode.UserExists => new AlreadyExistsException("Registration", null),
                SimpleTradingResultCode.InvalidUserNameOrPassword => new BadRequestException(
                    "Invalid username or password"),
                SimpleTradingResultCode.PersonalDataNotValid => new BadRequestException("Registration data not valid"),
                SimpleTradingResultCode.SystemError => new Exception("Brand Error"),
                _ => new Exception("Unknown Error")
            };
        }
    }
}
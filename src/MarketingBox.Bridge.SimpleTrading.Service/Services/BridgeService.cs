using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarketingBox.Bridge.SimpleTrading.Service.Services.Integrations;
using MarketingBox.Bridge.SimpleTrading.Service.Services.Integrations.Contracts.Enums;
using MarketingBox.Bridge.SimpleTrading.Service.Services.Integrations.Contracts.Requests;
using MarketingBox.Bridge.SimpleTrading.Service.Settings;
using MarketingBox.Integration.Bridge.Client;
using MarketingBox.Integration.Service.Domain.Registrations;
using MarketingBox.Integration.Service.Grpc.Models.Registrations;
using MarketingBox.Sdk.Common.Models;
using MarketingBox.Sdk.Common.Models.Grpc;
using Microsoft.Extensions.Logging;
using IntegrationBridge = MarketingBox.Integration.Service.Grpc.Models.Registrations.Contracts.Bridge;

namespace MarketingBox.Bridge.SimpleTrading.Service.Services
{
    public class BridgeService : IBridgeService
    {
        private readonly ILogger<BridgeService> _logger;
        private readonly ISimpleTradingHttpClient _simpleTradingHttpClient;
        private readonly SettingsModel _settingsModel;

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
            _logger.LogInformation("Creating new LeadInfo {@context}", request);

            var brandRequest = MapToApi(request, _settingsModel.BrandBrandId,
                Convert.ToInt32(_settingsModel.BrandAffiliateId), _settingsModel.BrandAffiliateKey,
                DateTimeOffset.UtcNow.ToString());

            try
            {
                return await RegisterExternalCustomerAsync(brandRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lead {@context}", request);
                
                return new Response<CustomerInfo>()
                {
                    Status = ResponseStatus.InternalError,
                    Error = new Error()
                    {
                        ErrorMessage = ex.Message
                    }
                };
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
                return new Response<CustomerInfo>()
                {
                    Status = ResponseStatus.InternalError,
                    Error = new Error()
                    {
                        ErrorMessage = "Brand response error"
                    }
                };
            }

            // Success
            if (registerResult.SuccessResult.IsSuccessfully() &&
                (SimpleTradingResultCode)registerResult.SuccessResult.Status ==
                SimpleTradingResultCode.Ok)
            {
                // Success
                return new Response<CustomerInfo>()
                {
                    Status = ResponseStatus.Ok,
                    Data = new CustomerInfo()
                    {
                        CustomerId = registerResult.SuccessResult.TraderId,
                        LoginUrl = registerResult.SuccessResult.RedirectUrl,
                        Token = registerResult.SuccessResult.Token
                    }
                };
            }

            // Success, but software failure
            if ((SimpleTradingResultCode)registerResult.SuccessResult.Status ==
                SimpleTradingResultCode.UserExists)
            {
                return new Response<CustomerInfo>()
                {
                    Status = ResponseStatus.InternalError,
                    Error = new Error()
                    {
                        ErrorMessage = "Registration already exists"
                    }
                };
            }

            if ((SimpleTradingResultCode)registerResult.SuccessResult.Status ==
                SimpleTradingResultCode.InvalidUserNameOrPassword)
            {
                return new Response<CustomerInfo>()
                {
                    Status = ResponseStatus.Unauthorized,
                    Error = new Error()
                    {
                        ErrorMessage = "Invalid username or password"
                    }
                };
            }

            if ((SimpleTradingResultCode)registerResult.SuccessResult.Status ==
                SimpleTradingResultCode.PersonalDataNotValid)
            {
                return new Response<CustomerInfo>()
                {
                    Status = ResponseStatus.BadRequest,
                    Error = new Error()
                    {
                        ErrorMessage = ((SimpleTradingResultCode)registerResult.SuccessResult.Status).ToString()
                    }
                };
            }

            if ((SimpleTradingResultCode)registerResult.SuccessResult.Status ==
                SimpleTradingResultCode.SystemError)
            {
                return new Response<CustomerInfo>()
                {
                    Status = ResponseStatus.InternalError,
                    Error = new Error()
                    {
                        ErrorMessage = ((SimpleTradingResultCode)registerResult.SuccessResult.Status).ToString()
                    }
                };
            }

            return new Response<CustomerInfo>()
            {
                Status = ResponseStatus.InternalError,
                Error = new Error()
                {
                    ErrorMessage = "Unknown Error"
                }
            };
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

        /// <summary>
        /// Get all registrations per period
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<Response<IReadOnlyCollection<RegistrationReporting>>> GetRegistrationsPerPeriodAsync(IntegrationBridge.ReportingRequest request)
        {
            _logger.LogInformation("GetRegistrationsPerPeriodAsync {@context}", request);

            var brandRequest = MapToApi(request, _settingsModel.BrandAffiliateKey);

            try
            {
                // Get registrations
                var registerResult = await _simpleTradingHttpClient.GetRegistrationsAsync(brandRequest);
                // Failed
                if (registerResult.IsFailed)
                {
                    return new Response<IReadOnlyCollection<RegistrationReporting>>()
                    {
                        Status = ResponseStatus.InternalError,
                        Error = new Error()
                        {
                            ErrorMessage = registerResult.FailedResult.Message
                        }
                    };
                }

                var registrations = registerResult.SuccessResult.Items
                    .Select(report => new RegistrationReporting
                {
                    Crm = MapCrmStatus(report.CrmStatus),
                    CustomerEmail = report.Email,
                    CustomerId = report.UserId,
                    CreatedAt = report.CreatedAt,
                    CrmUpdatedAt = DateTime.UtcNow
                }).ToList();
                
                // Success
                return new Response<IReadOnlyCollection<RegistrationReporting>>()
                {
                    Status = ResponseStatus.Ok,
                    Data = registrations
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error get registration reporting {@context}", request);
                return new Response<IReadOnlyCollection<RegistrationReporting>>()
                {
                    Status = ResponseStatus.InternalError,
                    Error = new Error()
                    {
                        ErrorMessage = ex.Message
                    }
                };
            }
        }

        private ReportRequest MapToApi(
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

        /// <summary>
        /// Get all deposits per period
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<Response<IReadOnlyCollection<DepositorReporting>>> GetDepositorsPerPeriodAsync(IntegrationBridge.ReportingRequest request)
        {
            _logger.LogInformation("GetRegistrationsPerPeriodAsync {@context}", request);

            var brandRequest = MapToApi(request, _settingsModel.BrandAffiliateKey);

            try
            {
                // Get deposits
                var depositsResult = await _simpleTradingHttpClient.GetDepositsAsync(brandRequest);
                // Failed
                if (depositsResult.IsFailed)
                {
                    return new Response<IReadOnlyCollection<DepositorReporting>>()
                    {
                        Status = ResponseStatus.InternalError,
                        Error = new Error()
                        {
                            ErrorMessage = depositsResult.FailedResult.Message
                        }
                    };
                }

                var registrations = depositsResult.SuccessResult.Items
                    .Select(report => new DepositorReporting
                {
                    CustomerEmail = report.Email,
                    CustomerId = report.UserId,
                    DepositedAt = report.CreatedAt,
                }).ToList();
                
                // Success
                return new Response<IReadOnlyCollection<DepositorReporting>>()
                {
                    Status = ResponseStatus.Ok,
                    Data = registrations
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error get registration reporting {@context}", request);

                return new Response<IReadOnlyCollection<DepositorReporting>>()
                {
                    Status = ResponseStatus.InternalError,
                    Error = new Error()
                    {
                        ErrorMessage = ex.Message
                    }
                };
            }
        }
    }
}
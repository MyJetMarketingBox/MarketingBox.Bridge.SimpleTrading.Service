using System.Threading.Tasks;
using MarketingBox.Integration.SimpleTrading.Bridge.Services.Integrations.Contracts.Requests;
using MarketingBox.Integration.SimpleTrading.Bridge.Services.Integrations.Contracts.Responses;

namespace MarketingBox.Integration.SimpleTrading.Bridge.Services.Integrations
{
    public interface ISimpleTradingHttpClient
    {
        /// <summary>
        /// A purchase deduct amount immediately. This transaction type is intended when the goods or services
        /// can be immediately provided to the customer. 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<Response<RegisterResponse, FailRegisterResponse>> RegisterTraderAsync(
            RegisterRequest request);

        /// <summary>
        /// It allows to get previous transaction basic information
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<Response<StatisticResponse, FailRegisterResponse>> GetReportStatisticAsync(
            StatisticRequest request);

    }
}
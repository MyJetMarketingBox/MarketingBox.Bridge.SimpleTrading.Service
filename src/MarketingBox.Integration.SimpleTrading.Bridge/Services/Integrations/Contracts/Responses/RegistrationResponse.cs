using MarketingBox.Integration.SimpleTrading.Bridge.Services.Integrations.Contracts.Enums;

namespace MarketingBox.Integration.SimpleTrading.Bridge.Services.Integrations.Contracts.Responses
{
    public class RegistrationResponse
    {
        public string TraderId { get; set; }
        public string Token { get; set; }
        public string RedirectUrl { get; set; }
        public /*OperationApiResponseCodes*/int Status { get; set; }


        public bool IsSuccessfully()
        {
            var status = (SimpleTradingResultCode)(Status);
            return status == SimpleTradingResultCode.Ok;
        }
    }
}
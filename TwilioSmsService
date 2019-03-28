using Microsoft.Extensions.Options;
using MyApp.Data.Providers;
using MyApp.Models.Domain;
using MyApp.Models.Requests;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace MyApp.Services
{
    public class SmsServices : ISmsServices
    {
        private readonly IDataProvider _dataProvider;
        private readonly SmsConfig _config;

        public SmsServices(IDataProvider dataProvider, IOptions<SmsConfig> options)
        {
            _dataProvider = dataProvider;
            _config = options.Value;

            TwilioClient.Init(_config.AccountSid, _config.AuthToken);
        }

        public object ConfirmSms(string phoneNumber, string smsToken)
        {
            SmsAddRequest smsAddRequest = new SmsAddRequest();
            smsAddRequest.Body = "Thank you for registering with MyApp! Here is your 4-digit token. " + smsToken;
            smsAddRequest.From = "+10000000000";
            smsAddRequest.To = phoneNumber;

            return SendSms(smsAddRequest);
        }

        private object SendSms(SmsAddRequest newMsg)
        {
            var msg = MessageResource.Create(
                from: newMsg.From,
                body: newMsg.Body, 
                to: newMsg.To
                );
            return msg;
        }

    }
}

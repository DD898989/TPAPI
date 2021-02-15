using System;
using TPAPI.Models;
using TPAPI.Models.Table;
using Newtonsoft.Json;
using NLog;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using System.Configuration;

namespace TPAPI.Provider
{
    public static class Twilio
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private static string AccountSid = ConfigurationManager.AppSettings["Twilio_AccountSid"];
        private static string AuthToken = ConfigurationManager.AppSettings["Twilio_AuthToken"];
        private static string FromPhone = ConfigurationManager.AppSettings["Twilio_FromPhone"];

        public static DataResult<dynamic> Send(SendModel model, out EProvider provider, out string sender)
        {
            provider = EProvider.Twilio;

            sender = FromPhone;
            try
            {
                TwilioClient.Init(AccountSid, AuthToken);

                var message = MessageResource.Create(
                    body: model.Content,
                    from: new PhoneNumber(sender),
                    to: new PhoneNumber(model.Receiver)//'The number  is unverified. Trial accounts cannot send messages to unverified numbers; verify  at twilio.com/user/account/phone-numbers/verified, or purchase a Twilio number to send messages to unverified numbers.
                );


                if (message.ErrorCode != null)
                {
                    logger.Error(JsonConvert.SerializeObject(model) +JsonConvert.SerializeObject(message) );

                    return DataResult<dynamic>.Fail(Code.第三方錯誤 ,"error code:" + message.ErrorCode + "   " + "error msg:" + message.ErrorMessage);
                }
                else
                {
                    return DataResult<dynamic>.Success();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex,JsonConvert.SerializeObject(model));


                return DataResult<dynamic>.Fail(Code.第三方錯誤 ,ex);
            }
        }
    }
}
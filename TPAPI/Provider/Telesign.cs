using System;
using NLog;
using TPAPI.Models;
using TPAPI.Models.Table;
using Newtonsoft.Json;
using Telesign;
using System.Configuration;

namespace TPAPI.Provider
{
    public class Telesign
    {
        public static string CustomerId = ConfigurationManager.AppSettings["Telesign_CustomerId"];
        public static string ApiKey = ConfigurationManager.AppSettings["Telesign_ApiKey"];

        public static DataResult<dynamic> Send(SendModel model, out EProvider provider, out string sender)
        {

            provider = EProvider.Telesign;
            sender = Const.Me;

            RestClient.TelesignResponse telesignResponse = null;

            try
            {
                MessagingClient messagingClient = new MessagingClient(CustomerId, ApiKey);
                telesignResponse = messagingClient.Message(model.Receiver, model.Content, "OTP");//Label the type of message you are sending. You have three choices: OTP - One time passwords, ARN - Alerts, reminders, and notifications, and MKT - Marketing traffic.
            }
            catch (Exception ex)
            {
                return DataResult<dynamic>.Fail(Code.第三方錯誤, ex);
            }

            if(telesignResponse.OK && telesignResponse.StatusCode== 200)
            {
                return DataResult<dynamic>.Success();
            }
            else
            {
                return DataResult<dynamic>.Fail(Code.第三方錯誤, JsonConvert.SerializeObject(telesignResponse));
            }
        }
    }
}
using System;
using System.Collections.Generic;
using TPAPI.Models;
using TPAPI.Models.Table;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Model;
using sib_api_v3_sdk.Client;
using NLog;

namespace TPAPI.Provider
{

    public class SendinBlue
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static string ApiKey = System.Configuration.ConfigurationManager.AppSettings["SendinBlue_ApiKey"];


        public static DataResult<dynamic> Send(SendModel model, out EProvider provider, out string sender)
        {
            provider = EProvider.SendinBlue;

            sender = Const.FromEmail;
            Configuration.Default.AddApiKey("api-key", ApiKey);
            Configuration.Default.AddApiKey("partner-key", ApiKey);

            try
            {
                var apiInstance = new SMTPApi();
                var sendSmtpEmail = new SendSmtpEmail(
                    new SendSmtpEmailSender(Const.Me, sender),
                    new List<SendSmtpEmailTo>() { new SendSmtpEmailTo(model.Receiver) },null,null, model.Content, model.Content, model.Title
                    ); 

                CreateSmtpEmail result = apiInstance.SendTransacEmail(sendSmtpEmail);
            }
            catch (Exception ex)
            {
                return DataResult<dynamic>.Fail(Code.第三方錯誤, ex);
            }

            return DataResult<dynamic>.Success();
        }
    }
}
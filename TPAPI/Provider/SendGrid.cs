using System.Configuration;
using TPAPI.Models;
using TPAPI.Models.Table;
using SendGrid;
using SendGrid.Helpers.Mail;
using NLog;
using Newtonsoft.Json;

namespace TPAPI.Provider
{
    public static class SendGrid
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static string ApiKey = ConfigurationManager.AppSettings["SendGrid_ApiKey"];

        public static DataResult<dynamic> Send(SendModel model, out EProvider provider, out string sender)
        {
            provider = EProvider.SendGrid;

            var client = new SendGridClient(ApiKey);


            sender = Const.FromEmail;
            var msg = MailHelper.CreateSingleEmail(
                new EmailAddress(sender, Const.Me),
                new EmailAddress(model.Receiver),
                model.Title,
                model.Content,
                model.Content
                );

            var response = client.SendEmailAsync(msg).Result;
            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                logger.Error(
                    JsonConvert.SerializeObject(model) +
                    JsonConvert.SerializeObject(msg)
                    );

                return DataResult<dynamic>.Fail(Code.第三方錯誤
                    , response.StatusCode.ToString() + "   " + response.Body.ReadAsStringAsync().Result);
            }
            else
            {
                return DataResult<dynamic>.Success();
            }
        }
    }
}
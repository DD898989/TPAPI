using System.Configuration;
using TPAPI.Models;
using TPAPI.Models.Table;
using FluentEmail.Core;
using FluentEmail.Mailgun;
using NLog;
using Newtonsoft.Json;

namespace TPAPI.Provider
{
    public class Mailgun //需要付費才能寄給非白名單的email
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private static string APIKey = ConfigurationManager.AppSettings["Mailgun_APIKey"];
        private static string Domain = ConfigurationManager.AppSettings["Mailgun_Domain"];

        public static DataResult<dynamic> Send(SendModel model, out EProvider provider, out string sender)
        {
            provider = EProvider.Mailgun;

            Email.DefaultSender = new MailgunSender(
                    Domain,
                    APIKey
            );

            sender = Const.FromEmail;

            var email = Email
            .From(sender)
            .To(model.Receiver)
            .Subject(model.Title)
            .Body(model.Content);

            var response =  email.Send();

            if (!response.Successful)
            {
                return DataResult<dynamic>.Fail(
                    Code.第三方錯誤,
                    JsonConvert.SerializeObject(response.ErrorMessages));
            }
            else
            {
                return DataResult<dynamic>.Success();
            }
        }
    }
}
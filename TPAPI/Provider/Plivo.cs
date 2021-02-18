using System;
using System.Configuration;
using System.Collections.Generic;
using TPAPI.Models;
using TPAPI.Models.Table;
using NLog;
using Plivo;
using Newtonsoft.Json;

namespace TPAPI.Provider
{   //
    public static class Plivo
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static string AuthID = ConfigurationManager.AppSettings["Plivo_AuthID"];
        public static string AuthToken = ConfigurationManager.AppSettings["Plivo_AuthToken"];
        public static string FromPhone = ConfigurationManager.AppSettings["Plivo_FromPhone"];
        public static DataResult<dynamic> Send(SendModel model, out EProvider provider, out string sender)
        {
            provider = EProvider.Plivo;
            sender = FromPhone;
            try
            {
                var api = new PlivoApi(AuthID, AuthToken);
                var response = api.Message.Create(
                    src: sender,
                    dst: new List<String> { model.Receiver },
                    text: model.Content
                );

                var responseJson = JsonConvert.SerializeObject(response);
                if (response.StatusCode != 200 && response.StatusCode != 202)
                {
                    var msg = "if (response.StatusCode != 200 && response.StatusCode != 202)" + responseJson;
                    logger.Error(msg);
                    return DataResult<dynamic>.Fail(Code.第三方錯誤, responseJson);
                }
                else
                {
                    logger.Info(responseJson);
                    return DataResult<dynamic>.Success();
                }
            }
            catch(Exception ex)
            {
                logger.Error(ex);
                return DataResult<dynamic>.Fail(Code.第三方錯誤, ex);
            }
        }
    }
}
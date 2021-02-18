using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Linq;
using System.Data.SqlClient;
using TPAPI.Models;
using TPAPI.Models.Table;
using TPAPI.Provider;
using NLog;
using Dapper;
using Newtonsoft.Json;

namespace TPAPI.Controllers
{
    public class MsgController : ApiController
    {
        private Logger logger = LogManager.GetLogger("logger" + Migrate.programVersion);
        public delegate DataResult<dynamic> SendDel(SendModel model, out EProvider provider, out string sender);

        public static DataResult<dynamic> Save(SendModel model, EProvider provider, EType type, string sender, DateTime dateTime)
        {
            var sql_insert = @"insert into " + nameof(Messages) + @" (" +

                nameof(Messages.brandID) + "," +
                nameof(Messages.datetime) + "," +
                nameof(Messages.mainAccountID) + "," +
                nameof(Messages.provider) + "," +
                nameof(Messages.receiver) + "," +
                nameof(Messages.sender) + "," +
                nameof(Messages.title) + "," +
                nameof(Messages.type) + "," +
                nameof(Messages.content) +

                ") values (" +

                model.BrandID + "," +
                "'" + dateTime.ToString(Messages.sqlTimeFormat()) + "'" + "," +
                "'" + model.MainAccountID + "'" + "," +
                (int)provider + "," +
                "'" + model.Receiver + "'" + "," +
                "N'" + sender + "'" + "," +
                "N'" + model.Title + "'" + "," +
                (int)type + "," +
                "N'" + model.Content + "'" +
                ")";

            try
            {
                using (SqlConnection conn = new SqlConnection(General.ConnString_TPDB()))
                {
                    conn.Open();
                    conn.Execute(sql_insert);
                }
            }
            catch (Exception ex)
            {
                return DataResult<dynamic>.Fail(Code.資料庫錯誤, ex);
            }


            return DataResult<dynamic>.Success();
        }
        private DataResult<dynamic> Load(GetModel model, EType type)
        {
            var andQuery =
                (true ? " and " +
                    nameof(Messages.brandID) + "=" + model.BrandID
                    : "")
                +
                (true ? " and " +
                    nameof(Messages.datetime) + ">" + "'" + ((DateTime)model.StartDate).ToString(Messages.sqlTimeFormat()) + "'"
                    : "")
                +
                (model.EndDate != null ? " and " +
                    nameof(Messages.datetime) + "<" + "'" + ((DateTime)model.EndDate).ToString(Messages.sqlTimeFormat()) + "'"
                    : "")
                +
                (model.MainAccountID != null ? " and " +
                    nameof(Messages.mainAccountID) + "=" + "'" + model.MainAccountID + "'"
                    : "")
                +
                (true ? " and " +
                    nameof(Messages.type) + "=" + (int)type
                    : "")
                ;



            var sql_multi = "";

            {

                var sql_count =
                    "select count(*) from " + nameof(Messages) + " where 1=1 "
                    +
                    andQuery;

                var sql_load =
                    "select * from " + nameof(Messages) + " where 1=1 "
                    +
                    andQuery
                    +
                    " order by " + nameof(Messages.datetime) + " offset " + model.PageNumber * model.RecordCount + " rows fetch next " + model.RecordCount + " rows only "
                    ;

                sql_multi =
                    sql_count + ";" + sql_load + ";";
            }



            int total = 0;
            List<MessagesLoad> data = null;

            try
            {
                using (SqlConnection conn = new SqlConnection(General.ConnString_TPDB()))
                {
                    conn.Open();

                    using (var multi = conn.QueryMultiple(sql_multi))
                    {
                        total = multi.ReadSingle<int>();
                        data = multi.Read<MessagesLoad>().ToList();
                    }

                    return DataResult<dynamic>.Success(
                        new Dictionary<string, object>()
                        {
                            { "total", total },
                            { "data", data },
                        }
                        );

                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, sql_multi);
                return DataResult<dynamic>.Fail(Code.資料庫錯誤, ex);
            }
        }

        private DataResult<dynamic> checkParameter(ref SendModel model)
        {
            if (!(model?.MainAccountID?.Length > 0)) { return DataResult<dynamic>.Fail(Code.參數錯誤, "parameter error: MainAccountID is empty"); }
            if (!(model?.Content?.Length > 0)) { return DataResult<dynamic>.Fail(Code.參數錯誤, "parameter error: Content is empty"); }
            if (!(model?.Receiver?.Length > 0)) { return DataResult<dynamic>.Fail(Code.參數錯誤, "parameter error: Receiver is empty"); }
            if (!(model?.BrandID > 0)) { return DataResult<dynamic>.Fail(Code.參數錯誤, "parameter error: BrandID is empty"); }
            if (!(model?.Title?.Length > 0)) { model.Title = ""; }


            return DataResult<dynamic>.Success();
        }

        private DataResult<dynamic> checkParameter(ref GetModel model)
        {
            if (!(model?.BrandID > 0)) { return DataResult<dynamic>.Fail(Code.參數錯誤, "parameter error: BrandID is empty"); }
            if (!(model?.RecordCount > 0)) { model.RecordCount = 10; }
            if (model.PageNumber == null) { model.PageNumber = 0; }
            if (model.StartDate == null) { model.StartDate = DateTime.Today.AddDays(-7); }

            return DataResult<dynamic>.Success();
        }

        private SendDel EmailDelegate(int resendTimes)
        {
            switch (resendTimes)
            {
                case 0:
                    return Provider.SendGrid.Send;
                case 1:
                    return Provider.Mailgun.Send;
                default:
                    return Provider.SendinBlue.Send;
            }
        }

        private SendDel SMSDelegate(int resendTimes)
        {
            switch (resendTimes)
            {
                case 0:
                    return Provider.Plivo.Send;
                case 1:
                    return Provider.Telesign.Send;
                default:
                    return Provider.Twilio.Send;
            }
        }


        [HttpPost]
        public DataResult<dynamic> SendEmail(SendModel model)
        {
            if (model.ResendTimes == null) { return DataResult<dynamic>.Fail(Code.參數錯誤, "parameter error: ResendTimes is empty"); }

            logger.Info(JsonConvert.SerializeObject(model));

            var check = checkParameter(ref model);
            if (check.code != Code.成功)
            {
                return check;
            }

            var send = EmailDelegate((int)model.ResendTimes)(model, out EProvider provider, out string sender);
            if (send.code != Code.成功)
            {
                return send;
            }

            var save = Save(model, provider, EType.Mail, sender, DateTime.Now);
            if (save.code != Code.成功)
            {
                return save;
            }

            return DataResult<dynamic>.Success();
        }

        [HttpPost]
        public DataResult<dynamic> SendSMS(SendModel model)
        {
            if (model.ResendTimes == null) { return DataResult<dynamic>.Fail(Code.參數錯誤, "parameter error: ResendTimes is empty"); }

            logger.Info(JsonConvert.SerializeObject(model));

            var check = checkParameter(ref model);
            if (check.code != Code.成功)
            {
                return check;
            }

            var send = SMSDelegate((int)model.ResendTimes)(model, out EProvider provider, out string sender);
            if (send.code != Code.成功)
            {
                return send;
            }

            var save = Save(model, provider, EType.SMS, sender, DateTime.Now);
            if (save.code != Code.成功)
            {
                return save;
            }

            return DataResult<dynamic>.Success();
        }

        [HttpGet]
        public DataResult<dynamic> GetEmail(GetModel model)
        {
            logger.Info(JsonConvert.SerializeObject(model));

            var check = checkParameter(ref model);
            if (check.code != Code.成功)
            {
                return check;
            }

            return Load(model, EType.Mail);
        }

        [HttpGet]
        public DataResult<dynamic> GetSMS(GetModel model)
        {
            logger.Info(JsonConvert.SerializeObject(model));

            var check = checkParameter(ref model);
            if (check.code != Code.成功)
            {
                return check;
            }

            return Load(model, EType.SMS);
        }


        [HttpGet]
        public DataResult<dynamic> GetCode()
        {
            var rtn = Enum.GetValues(typeof(Code))
               .Cast<Code>()
               .ToDictionary(t => (int)t, t => t.ToString());

            return DataResult<dynamic>.Success(rtn);
        }
    }
}
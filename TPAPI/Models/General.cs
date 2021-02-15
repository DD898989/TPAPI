using System;
using System.Collections.Generic;
using System.Configuration;

namespace TPAPI.Models
{
    public static class Const
    {
        public static string FromEmail = ConfigurationManager.AppSettings["FromEmail"];
        public static string Me = "My Service Name";
    }

    public static class General
    {
        public static string ConnString_TPDB()
        {
            return ConfigurationManager.ConnectionStrings["TPDB"].ConnectionString;
        }
    }


    public class SendModel
    {
        public int BrandID { get; set; }
        public string MainAccountID { get; set; }

        public int? ResendTimes { get; set; }


        public string Receiver { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
    }


    public class GetModel
    {
        public int BrandID { get; set; }
        public string MainAccountID { get; set; }

        public int? PageNumber { get; set; }
        public int? RecordCount { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public enum Code
    {
        //通用
        TP失敗 = -1,
        TP待定義 = 0,
        TP成功 = 1,

        //資料庫相關
        資料庫錯誤 = 5000,
        查無該筆資料 = 5001,


        //參數相關
        參數錯誤 = 6000,
        Json轉Object失敗 = 6001,

        //服務溝通錯誤
        第三方錯誤 = 7000,
    }

    public class DataResult<T>
    {
        public virtual Code code { get; set; }
        public virtual string codeName { get; set; }
        public virtual object errorData { get; set; }
        public virtual T successData { get; set; }

        static public DataResult<dynamic> Success(T successData)
        {
            return new DataResult<dynamic>(Code.TP成功, successData, null);
        }

        static public DataResult<dynamic> Success()
        {
            return new DataResult<dynamic>(Code.TP成功, null, null);
        }

        static public DataResult<dynamic> Fail(Code code, object errorData)
        {
            return new DataResult<dynamic>(code, null, errorData);
        }

        static public DataResult<dynamic> Fail(Code code, Exception exception)
        {
            return new DataResult<dynamic>(code, null, exception);
        }

        static public DataResult<dynamic> Fail(Code code)
        {
            return new DataResult<dynamic>(code, null, null);
        }

        private DataResult(Code code, T successData, object errorData)
        {
            this.code = code;
            this.codeName = code.ToString();
            this.successData = successData;
            this.errorData = errorData;
        }
    }

}